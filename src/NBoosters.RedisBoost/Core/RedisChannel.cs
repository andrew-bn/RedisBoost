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
using NBoosters.RedisBoost.Core.Serialization;

namespace NBoosters.RedisBoost.Core
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

		private volatile AsyncOperationDelegate<Exception, RedisResponse> _receiveCallBack;
		private volatile int _receiveMultiBulkPartsLeft;
		private volatile RedisResponse[] _multiBulkParts;

		public bool ReadResponseAsync(AsyncOperationDelegate<Exception, RedisResponse> callBack)
		{
			_receiveCallBack = callBack;
			_receiveMultiBulkPartsLeft = 0;
			_multiBulkParts = null;

			return ReadResponseFromStream(true);
		}
		private bool ReadResponseFromStream(bool sync)
		{
			return _redisStream.ReadLine((s,e,l)=>ProcessRedisLine(s&&sync,e,l));
		}
		private bool ProcessRedisResponse(bool sync, Exception ex, RedisResponse response)
		{
			if (ex != null)
				return _receiveCallBack(sync, ex, null) && sync;

			if (_multiBulkParts == null)
				return _receiveCallBack(sync, null, response) && sync;

			if (_receiveMultiBulkPartsLeft > 0)
				_multiBulkParts[_multiBulkParts.Length - _receiveMultiBulkPartsLeft] = response;

			--_receiveMultiBulkPartsLeft;

			return _receiveMultiBulkPartsLeft <= 0
					   ? _receiveCallBack(sync, null, RedisResponse.CreateMultiBulk(_multiBulkParts, _serializer)) && sync
					   : ReadResponseFromStream(sync) && sync;
		}
		private bool ProcessRedisBulkLine(bool sync, Exception ex, byte[] data)
		{
			return ex != null
					   ? ProcessRedisResponse(sync, ex, null) && sync
					   : ProcessRedisResponse(sync, null, RedisResponse.CreateBulk(data, _serializer)) && sync;
		}

		private bool ProcessRedisLine(bool sync, Exception ex, RedisLine line)
		{
			if (ex != null)
				return ProcessRedisResponse(sync, ex, null) && sync;

			if (_redisDataAnalizer.IsErrorReply(line.FirstChar))
				return ProcessRedisResponse(sync, null, RedisResponse.CreateError(line.Line, _serializer)) && sync;
			if (_redisDataAnalizer.IsStatusReply(line.FirstChar))
				return ProcessRedisResponse(sync, null, RedisResponse.CreateStatus(line.Line, _serializer)) && sync;
			if (_redisDataAnalizer.IsIntReply(line.FirstChar))
				return ProcessRedisResponse(sync, null, RedisResponse.CreateInteger(_redisDataAnalizer.ConvertToLong(line.Line), _serializer)) && sync;
			if (_redisDataAnalizer.IsBulkReply(line.FirstChar))
			{
				var length = _redisDataAnalizer.ConvertToInt(line.Line);
				//check nil reply
				return length == -1
						   ? ProcessRedisResponse(sync, null, RedisResponse.CreateBulk(null, _serializer)) && sync
					       : _redisStream.ReadBlockLine(length, (s, err, data) => ProcessRedisBulkLine(s && sync, err, data)) && sync;
			}
			if (_redisDataAnalizer.IsMultiBulkReply(line.FirstChar))
			{
				_receiveMultiBulkPartsLeft = _redisDataAnalizer.ConvertToInt(line.Line);

				if (_receiveMultiBulkPartsLeft == -1) // multi-bulk nill
					return ProcessRedisResponse(sync, null, RedisResponse.CreateMultiBulk(null, _serializer)) && sync;

				_multiBulkParts = new RedisResponse[_receiveMultiBulkPartsLeft];

				if (_receiveMultiBulkPartsLeft == 0)
					return ProcessRedisResponse(sync, null, RedisResponse.CreateMultiBulk(_multiBulkParts, _serializer)) && sync;

				return ReadResponseFromStream(sync) && sync;
			}
			return ProcessRedisResponse(sync, new RedisException("Invalid reply type"), null) && sync;
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
