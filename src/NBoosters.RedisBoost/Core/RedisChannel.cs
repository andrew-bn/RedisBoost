using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
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
		public Task SendAsync(byte[][] request)
		{
			var tcs = new TaskCompletionSource<bool>();
			_sendCallBack = ex =>
				{
					if (ex!=null)
						tcs.SetException(ex);
					else tcs.SetResult(true);
				};
			_sendRequest = request;
			_sendState = 0;
			_partIndex = 0;
			SendDataTask(null);
			return tcs.Task;
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

		public Task<RedisResponse> ReadResponseAsync()
		{
			var tcs = new TaskCompletionSource<RedisResponse>();
			
			_receiveCallBack = (ex, r) =>
				{
					if (ex!=null)
						tcs.SetException(ex);
					else tcs.SetResult(r);
				};
			_receiveMultiBulkPartsLeft = 0;
			_multiBulkParts = null;

			ReadResponseTask(FinishResponseReading);

			return tcs.Task;
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

				_multiBulkParts = new RedisResponse[_receiveMultiBulkPartsLeft];

				if (_receiveMultiBulkPartsLeft == 0)
					continuation(null, RedisResponse.CreateMultiBulk(_multiBulkParts, _serializer));
				else
					ReadResponseTask(FinishResponseReading);
			}
			else continuation(new RedisException("Invalid reply type"), null);
		}
		#endregion
		public Task Flush()
		{
			var tcs = new TaskCompletionSource<bool>();
			_redisStream.Flush(ex =>
				{
					if (ex != null)
						tcs.SetException(ex);
					else tcs.SetResult(true);
				});
			return tcs.Task;
		}
		public Task ConnectAsync(EndPoint endPoint)
		{
			return _redisStream.Connect(endPoint);
		}

		public Task DisconnectAsync()
		{
			return _redisStream.Disconnect();
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
