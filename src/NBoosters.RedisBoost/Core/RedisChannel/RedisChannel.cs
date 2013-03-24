#region Apache Licence, Version 2.0
/*
 Copyright 2013 Andrey Bulygin.

 Licensed under the Apache License, Version 2.0 (the "License"); 
 you may not use this file except in compliance with the License. 
 You may obtain a copy of the License at 

		http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software 
 distributed under the License is distributed on an "AS IS" BASIS, 
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
 See the License for the specific language governing permissions 
 and limitations under the License.
 */
#endregion

using System;
using System.Net;
using System.Net.Sockets;
using NBoosters.RedisBoost.Core.RedisStream;
using NBoosters.RedisBoost.Core.Serialization;

namespace NBoosters.RedisBoost.Core.RedisChannel
{
	internal class RedisChannel : IRedisChannel
	{
		private readonly IRedisStream _redisStream;

		private readonly IRedisDataAnalizer _redisDataAnalizer;
		private IRedisSerializer _serializer;
		public IRedisDataAnalizer RedisDataAnalizer
		{
			get { return _redisDataAnalizer; }
		}

		public RedisChannel(IRedisStream redisStream,
			IRedisDataAnalizer redisDataAnalizer)
		{
			_redisStream = redisStream;
			_redisDataAnalizer = redisDataAnalizer;
		}

		public void EngageWith(Socket socket, IRedisSerializer serializer)
		{
			_redisStream.EngageWith(socket);
			_serializer = serializer;
		}

		#region Send Data Task

		private volatile int _sendState;
		private volatile byte[][] _sendRequest;
		private volatile int _partIndex;
		private ArraySegment<byte> _arraySegment;
		private volatile AsyncOperationDelegate<Exception> _sendCallBack;

		public bool SendAsync(byte[][] request, AsyncOperationDelegate<Exception> callback)
		{
			_sendCallBack = callback;
			_sendRequest = request;
			_sendState = 0;
			_partIndex = 0;
			return SendDataTask(true, null);
		}

		private bool SendDataTask(bool sync, Exception ex)
		{
			if (ex != null)
				return _sendCallBack(sync, ex) && sync;

			if (_sendState == 0)
			{
				if (!_redisStream.WriteArgumentsCountLine(_sendRequest.Length))
					return _redisStream.Flush(SendDataTask) && sync;

				_sendState = 1;
			}

			if (_sendState > 0)
			{
				for (; _partIndex < _sendRequest.Length; _partIndex++)
				{
					if (_sendState == 1)
					{
						if (!_redisStream.WriteDataSizeLine(_sendRequest[_partIndex].Length))
							return _redisStream.Flush(SendDataTask) && sync;

						_sendState = 2;
						_arraySegment = new ArraySegment<byte>(_sendRequest[_partIndex], 0, _sendRequest[_partIndex].Length);
					}

					if (_sendState == 2)
					{
						while (true)
						{
							_arraySegment = _redisStream.WriteData(_arraySegment);
							if (_arraySegment.Count > 0)
								return _redisStream.Flush(SendDataTask) && sync;

							_sendState = 3;
							break;
						}
					}
					if (_sendState == 3)
					{
						if (!_redisStream.WriteNewLine())
							return _redisStream.Flush(SendDataTask) && sync;

						_sendState = 1;
					}
				}
				return _sendCallBack(sync, null) && sync;
			}
			return sync;
		}
		#endregion
		#region Receive Data Task

		
		private volatile ChannelAsyncEventArgs _curReadChannelArgs;
		private readonly StreamAsyncEventArgs _readStreamArgs = new StreamAsyncEventArgs();
		public bool ReadResponseAsync(ChannelAsyncEventArgs args)
		{
			_curReadChannelArgs = args;
			_curReadChannelArgs.RedisResponse = null;
			_curReadChannelArgs.ReceiveMultiBulkPartsLeft = 0;
			_curReadChannelArgs.MultiBulkParts = null;
			_curReadChannelArgs.Exception = null;
			return ReadResponseFromStream(false);
		}

		private bool ReadResponseFromStream(bool async)
		{
			_readStreamArgs.Completed = null;
			_readStreamArgs.Completed = ProcessRedisLine;

			return _redisStream.ReadLine(_readStreamArgs) || ProcessRedisLine(async, _readStreamArgs);
		}

		private bool ProcessRedisResponse(bool async)
		{
			if (_curReadChannelArgs.Exception != null || _curReadChannelArgs.MultiBulkParts == null)
				return CallReadOnCompleted(async);

			if (_curReadChannelArgs.ReceiveMultiBulkPartsLeft > 0)
				_curReadChannelArgs.MultiBulkParts[_curReadChannelArgs.MultiBulkParts.Length - _curReadChannelArgs.ReceiveMultiBulkPartsLeft] = _curReadChannelArgs.RedisResponse;

			--_curReadChannelArgs.ReceiveMultiBulkPartsLeft;

			if (_curReadChannelArgs.ReceiveMultiBulkPartsLeft > 0)
				return ReadResponseFromStream(async);

			_curReadChannelArgs.RedisResponse = RedisResponse.CreateMultiBulk(_curReadChannelArgs.MultiBulkParts, _serializer);
			return CallReadOnCompleted(async);
		}

		private void ProcessRedisBulkLine(StreamAsyncEventArgs args)
		{
			ProcessRedisBulkLine(true,args);
		}

		private bool ProcessRedisBulkLine(bool async, StreamAsyncEventArgs args)
		{
			_curReadChannelArgs.Exception = args.Exception;

			if (args.HasException)
				return ProcessRedisResponse(async);

			_curReadChannelArgs.RedisResponse = RedisResponse.CreateBulk(args.Block, _serializer);
			return ProcessRedisResponse(async);
		}

		private void ProcessRedisLine(StreamAsyncEventArgs streamArgs)
		{
			ProcessRedisLine(true,streamArgs);
		}

		private bool ProcessRedisLine(bool async, StreamAsyncEventArgs streamArgs)
		{
			_curReadChannelArgs.Exception = streamArgs.Exception;

			if (streamArgs.HasException)
				return ProcessRedisResponse(async);

			if (_redisDataAnalizer.IsErrorReply(streamArgs.FirstChar))
				_curReadChannelArgs.RedisResponse = RedisResponse.CreateError(streamArgs.Line, _serializer);
			else if (_redisDataAnalizer.IsStatusReply(streamArgs.FirstChar))
				_curReadChannelArgs.RedisResponse = RedisResponse.CreateStatus(streamArgs.Line, _serializer);
			else if (_redisDataAnalizer.IsIntReply(streamArgs.FirstChar))
				_curReadChannelArgs.RedisResponse = RedisResponse.CreateInteger(_redisDataAnalizer.ConvertToLong(streamArgs.Line), _serializer);
			else if (_redisDataAnalizer.IsBulkReply(streamArgs.FirstChar))
			{
				var length = _redisDataAnalizer.ConvertToInt(streamArgs.Line);
				//check nil reply
				if (length < 0)
					_curReadChannelArgs.RedisResponse = RedisResponse.CreateBulk(null, _serializer);
				else if (length == 0)
					_curReadChannelArgs.RedisResponse = RedisResponse.CreateBulk(new byte[0], _serializer);
				else
				{
					_readStreamArgs.Length = length;
					_readStreamArgs.Completed = ProcessRedisBulkLine;
					return _redisStream.ReadBlockLine(_readStreamArgs) || 
							ProcessRedisBulkLine(async, _readStreamArgs);
				}
			}
			else if (_redisDataAnalizer.IsMultiBulkReply(streamArgs.FirstChar))
			{
				_curReadChannelArgs.ReceiveMultiBulkPartsLeft = _redisDataAnalizer.ConvertToInt(streamArgs.Line);

				if (_curReadChannelArgs.ReceiveMultiBulkPartsLeft == -1) // multi-bulk nill
					_curReadChannelArgs.RedisResponse = RedisResponse.CreateMultiBulk(null, _serializer);
				else
				{
					_curReadChannelArgs.MultiBulkParts = new RedisResponse[_curReadChannelArgs.ReceiveMultiBulkPartsLeft];

					if (_curReadChannelArgs.ReceiveMultiBulkPartsLeft > 0)
						return ReadResponseFromStream(async);
				}
			}

			return ProcessRedisResponse(async);
		}
		private bool CallReadOnCompleted(bool async)
		{
			if (async) _curReadChannelArgs.Completed(_curReadChannelArgs);
			return async;
		}
		#endregion
		public bool Flush(AsyncOperationDelegate<Exception> callBack)
		{
			return _redisStream.Flush(callBack);
		}
		public bool ConnectAsync(EndPoint endPoint, AsyncOperationDelegate<Exception> callBack)
		{
			return _redisStream.Connect(endPoint, callBack);
		}

		public bool DisconnectAsync(AsyncOperationDelegate<Exception> callBack)
		{
			return _redisStream.Disconnect(callBack);
		}

		public void Dispose()
		{
			_redisStream.DisposeAndReuse();
		}


		public bool BufferIsEmpty
		{
			get { return _redisStream.BufferIsEmpty; }
		}
	}
}
