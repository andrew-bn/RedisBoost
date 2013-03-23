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
		private volatile Action<Exception> _sendCallBack;
		public void SendAsync(byte[][] request, Action<Exception> callback)
		{
			_sendCallBack = callback;
			_sendRequest = request;
			_sendState = 0;
			_partIndex = 0;
			SendDataTask(null);
		}

		private void SendDataTask(Exception ex)
		{
			if (ex != null)
			{
				_sendCallBack(ex);
				return;
			}
			if (_sendState == 0)
			{
				if (!_redisStream.WriteArgumentsCountLine(_sendRequest.Length))
				{
					_redisStream.Flush(SendDataTask);
					return;
				}
				_sendState = 1;
			}

			if (_sendState > 0)
			{
				for (; _partIndex < _sendRequest.Length; _partIndex++)
				{
					if (_sendState == 1)
					{
						if (!_redisStream.WriteDataSizeLine(_sendRequest[_partIndex].Length))
						{
							_redisStream.Flush(SendDataTask);
							return;
						}
						_sendState = 2;
						_arraySegment = new ArraySegment<byte>(_sendRequest[_partIndex], 0, _sendRequest[_partIndex].Length);
					}

					if (_sendState == 2)
					{
						while (true)
						{
							_arraySegment = _redisStream.WriteData(_arraySegment);
							if (_arraySegment.Count > 0)
							{
								_redisStream.Flush(SendDataTask);
								return;
							}
							_sendState = 3;
							break;
						}
					}
					if (_sendState == 3)
					{
						if (!_redisStream.WriteNewLine())
						{
							_redisStream.Flush(SendDataTask);
							return;
						}
						_sendState = 1;
					}
				}
				_sendCallBack(null);
			}
		}
		#endregion
		#region Receive Data Task

		private volatile Action<Exception, RedisResponse> _receiveCallBack;
		private volatile int _receiveMultiBulkPartsLeft;
		private volatile RedisResponse[] _multiBulkParts;

		public void ReadResponseAsync(Action<Exception, RedisResponse> callBack)
		{
			_receiveCallBack = callBack;
			_receiveMultiBulkPartsLeft = 0;
			_multiBulkParts = null;

			ReadResponseTask(FinishResponseReading);
		}
		private void ReadResponseTask(Action<Exception, RedisResponse> callBack)
		{
			_redisStream.ReadLine((ex, line) => ProcessRedisLine(ex, line, callBack));
		}
		private void FinishResponseReading(Exception ex, RedisResponse response)
		{
			if (ex != null)
			{
				_receiveCallBack(ex, null);
				return;
			}

			if (_multiBulkParts == null)
				_receiveCallBack(null, response);
			else
			{
				if (_receiveMultiBulkPartsLeft > 0)
					_multiBulkParts[_multiBulkParts.Length - _receiveMultiBulkPartsLeft] = response;

				--_receiveMultiBulkPartsLeft;

				if (_receiveMultiBulkPartsLeft <= 0)
					_receiveCallBack(null, RedisResponse.CreateMultiBulk(_multiBulkParts, _serializer));
				else
					ReadResponseTask(FinishResponseReading);
			}

		}
		private void ProcessRedisBulk(Exception ex, byte[] data, Action<Exception, RedisResponse> continuation)
		{
			if (ex != null)
			{
				continuation(ex, null);
				return;
			}

			continuation(null, RedisResponse.CreateBulk(data, _serializer));
		}
		private void ProcessRedisLine(Exception ex, RedisLine line, Action<Exception, RedisResponse> continuation)
		{
			if (ex != null)
			{
				continuation(ex, null);
				return;
			}

			if (_redisDataAnalizer.IsErrorReply(line.FirstChar))
				continuation(null, RedisResponse.CreateError(line.Line, _serializer));
			else if (_redisDataAnalizer.IsStatusReply(line.FirstChar))
				continuation(null, RedisResponse.CreateStatus(line.Line, _serializer));
			else if (_redisDataAnalizer.IsIntReply(line.FirstChar))
				continuation(null, RedisResponse.CreateInteger(_redisDataAnalizer.ConvertToLong(line.Line), _serializer));
			else if (_redisDataAnalizer.IsBulkReply(line.FirstChar))
			{
				var length = _redisDataAnalizer.ConvertToInt(line.Line);
				//check nil reply
				if (length == -1)
					continuation(null, RedisResponse.CreateBulk(null, _serializer));
				else
					_redisStream.ReadBlockLine(length, (err, data) => ProcessRedisBulk(err, data, continuation));
			}
			else if (_redisDataAnalizer.IsMultiBulkReply(line.FirstChar))
			{
				_receiveMultiBulkPartsLeft = _redisDataAnalizer.ConvertToInt(line.Line);

				if (_receiveMultiBulkPartsLeft == -1) // multi-bulk nill
					continuation(null, RedisResponse.CreateMultiBulk(null, _serializer));
				else
				{
					_multiBulkParts = new RedisResponse[_receiveMultiBulkPartsLeft];

					if (_receiveMultiBulkPartsLeft == 0)
						continuation(null, RedisResponse.CreateMultiBulk(_multiBulkParts, _serializer));
					else
						ReadResponseTask(FinishResponseReading);
				}
			}
			else continuation(new RedisException("Invalid reply type"), null);
		}
		#endregion
		public void Flush(Action<Exception> callBack)
		{
			_redisStream.Flush(callBack);
		}
		public void ConnectAsync(EndPoint endPoint, Action<Exception> callBack)
		{
			_redisStream.Connect(endPoint, callBack);
		}

		public void DisconnectAsync(Action<Exception> callBack)
		{
			_redisStream.Disconnect(callBack);
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
