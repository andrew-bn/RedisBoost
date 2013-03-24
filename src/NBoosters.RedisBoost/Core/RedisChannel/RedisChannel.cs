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
		private volatile int _partIndex;
		private ArraySegment<byte> _arraySegment;
		private volatile ChannelAsyncEventArgs _curSendChannelArgs;
		private readonly StreamAsyncEventArgs _flushStreamArgs = new StreamAsyncEventArgs();
		public bool SendAsync(ChannelAsyncEventArgs args)
		{
			_curSendChannelArgs = args;
			_sendState = 0;
			_partIndex = 0;
			_flushStreamArgs.Completed = FlushCallBack;
			return SendDataTask(false);
		}
		private void FlushCallBack(StreamAsyncEventArgs args)
		{
			_curSendChannelArgs.Exception = args.Exception;
			SendDataTask(true);
		}
		private bool SendDataTask(bool async)
		{
			if (_curSendChannelArgs.HasException)
				return CallOnSendCompleted(async);

			if (_sendState == 0)
			{
				if (!_redisStream.WriteArgumentsCountLine(_curSendChannelArgs.SendData.Length))
					return _redisStream.Flush(_flushStreamArgs) || SendDataTask(async);

				_sendState = 1;
			}

			if (_sendState > 0)
			{
				for (; _partIndex < _curSendChannelArgs.SendData.Length; _partIndex++)
				{
					if (_sendState == 1)
					{
						if (!_redisStream.WriteDataSizeLine(_curSendChannelArgs.SendData[_partIndex].Length))
							return _redisStream.Flush(_flushStreamArgs) || SendDataTask(async);

						_sendState = 2;
						_arraySegment = new ArraySegment<byte>(_curSendChannelArgs.SendData[_partIndex], 0, _curSendChannelArgs.SendData[_partIndex].Length);
					}

					if (_sendState == 2)
					{
						while (true)
						{
							_arraySegment = _redisStream.WriteData(_arraySegment);
							if (_arraySegment.Count > 0)
								return _redisStream.Flush(_flushStreamArgs) || SendDataTask(async);

							_sendState = 3;
							break;
						}
					}
					if (_sendState == 3)
					{
						if (!_redisStream.WriteNewLine())
							return _redisStream.Flush(_flushStreamArgs) || SendDataTask(async);

						_sendState = 1;
					}
				}
			}
			return CallOnSendCompleted(async);
		}

		private bool CallOnSendCompleted(bool async)
		{
			if (async) _curSendChannelArgs.Completed(_curSendChannelArgs);
			return async;
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
				return CallOnReadCompleted(async);

			if (_curReadChannelArgs.ReceiveMultiBulkPartsLeft > 0)
				_curReadChannelArgs.MultiBulkParts[_curReadChannelArgs.MultiBulkParts.Length - _curReadChannelArgs.ReceiveMultiBulkPartsLeft] = _curReadChannelArgs.RedisResponse;

			--_curReadChannelArgs.ReceiveMultiBulkPartsLeft;

			if (_curReadChannelArgs.ReceiveMultiBulkPartsLeft > 0)
				return ReadResponseFromStream(async);

			_curReadChannelArgs.RedisResponse = RedisResponse.CreateMultiBulk(_curReadChannelArgs.MultiBulkParts, _serializer);
			return CallOnReadCompleted(async);
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
		private bool CallOnReadCompleted(bool async)
		{
			if (async) _curReadChannelArgs.Completed(_curReadChannelArgs);
			return async;
		}
		#endregion
		public bool Flush(ChannelAsyncEventArgs args)
		{
			_curSendChannelArgs = args;
			Action<StreamAsyncEventArgs> callBack = a =>
				{
					_curReadChannelArgs.Exception = a.Exception;
					CallOnSendCompleted(true);
				};
			_flushStreamArgs.Completed = callBack;
			var isAsync = _redisStream.Flush(_flushStreamArgs);
			if (!isAsync)
			{
				_curReadChannelArgs.Exception = _flushStreamArgs.Exception;
				CallOnSendCompleted(false);
			}
			return isAsync;
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
