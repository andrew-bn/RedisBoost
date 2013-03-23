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

			return ReadResponseTask(FinishResponseReading);
		}
		private bool ReadResponseTask(AsyncOperationDelegate<Exception, RedisResponse> callBack)
		{
			return _redisStream.ReadLine((s, ex, line) => ProcessRedisLine(s, ex, line, callBack));
		}
		private bool FinishResponseReading(bool sync, Exception ex, RedisResponse response)
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
					   : ReadResponseTask((s,e,r)=>FinishResponseReading(s&&sync,e,r)) && sync;
		}
		private bool ProcessRedisBulk(bool sync, Exception ex, byte[] data, AsyncOperationDelegate<Exception, RedisResponse> continuation)
		{
			return ex != null
				       ? continuation(sync, ex, null) && sync
				       : continuation(sync, null, RedisResponse.CreateBulk(data, _serializer)) && sync;
		}

		private bool ProcessRedisLine(bool sync, Exception ex, RedisLine line, AsyncOperationDelegate<Exception, RedisResponse> continuation)
		{
			if (ex != null)
				return continuation(sync, ex, null) && sync;

			if (_redisDataAnalizer.IsErrorReply(line.FirstChar))
				return continuation(sync, null, RedisResponse.CreateError(line.Line, _serializer)) && sync;
			if (_redisDataAnalizer.IsStatusReply(line.FirstChar))
				return continuation(sync, null, RedisResponse.CreateStatus(line.Line, _serializer)) && sync;
			if (_redisDataAnalizer.IsIntReply(line.FirstChar))
				return continuation(sync, null, RedisResponse.CreateInteger(_redisDataAnalizer.ConvertToLong(line.Line), _serializer)) && sync;
			if (_redisDataAnalizer.IsBulkReply(line.FirstChar))
			{
				var length = _redisDataAnalizer.ConvertToInt(line.Line);
				//check nil reply
				return length == -1
					       ? continuation(sync, null, RedisResponse.CreateBulk(null, _serializer)) && sync
					       : _redisStream.ReadBlockLine(length, (s, err, data) => ProcessRedisBulk(s && sync, err, data, continuation)) && sync;
			}
			if (_redisDataAnalizer.IsMultiBulkReply(line.FirstChar))
			{
				_receiveMultiBulkPartsLeft = _redisDataAnalizer.ConvertToInt(line.Line);

				if (_receiveMultiBulkPartsLeft == -1) // multi-bulk nill
					return continuation(sync, null, RedisResponse.CreateMultiBulk(null, _serializer)) && sync;

				_multiBulkParts = new RedisResponse[_receiveMultiBulkPartsLeft];

				if (_receiveMultiBulkPartsLeft == 0)
					return continuation(sync, null, RedisResponse.CreateMultiBulk(_multiBulkParts, _serializer)) && sync;

				return ReadResponseTask((s,err,data) => FinishResponseReading(s && sync, err, data)) && sync;
			}
			return continuation(sync, new RedisException("Invalid reply type"), null) && sync;
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
