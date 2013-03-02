using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NBoosters.RedisBoost.Core
{
	internal class RedisChannel : IRedisChannel
	{
		private readonly IRedisStream _redisStream;

		private readonly IRedisDataAnalizer _redisDataAnalizer;

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

		public void EngageWith(Socket socket)
		{
			_redisStream.EngageWith(socket);
		}

		public async Task SendAsync(byte[][] request)
		{
			if (!_redisStream.WriteArgumentsCountLine(request.Length))
			{
				await _redisStream.Flush().ConfigureAwait(false);
				_redisStream.WriteArgumentsCountLine(request.Length);
			}

			for (int i = 0; i < request.Length; i++)
			{
				if (!_redisStream.WriteDataSizeLine(request[i].Length))
				{
					await _redisStream.Flush().ConfigureAwait(false);
					_redisStream.WriteDataSizeLine(request[i].Length);
				}
				var arraySegment = new ArraySegment<byte>(request[i], 0, request[i].Length);
				while (true)
				{
					arraySegment = _redisStream.WriteData(arraySegment);
					if (arraySegment.Count == 0) break;
					await _redisStream.Flush().ConfigureAwait(false);
				}

				if (!_redisStream.WriteNewLine())
				{
					await _redisStream.Flush().ConfigureAwait(false);
					_redisStream.WriteNewLine();
				}
			}
			await _redisStream.Flush().ConfigureAwait(false);
		}
		public async Task<RedisResponse> ReadResponseAsync()
		{
			var line = await _redisStream.ReadLine().ConfigureAwait(false);
			if (_redisDataAnalizer.IsErrorReply(line.FirstChar))
				return RedisResponse.CreateError(line.Line);
			if (_redisDataAnalizer.IsStatusReply(line.FirstChar))
				return RedisResponse.CreateStatus(line.Line);
			if (_redisDataAnalizer.IsIntReply(line.FirstChar))
				return RedisResponse.CreateInteger(_redisDataAnalizer.ConvertToLong(line.Line));
			if (_redisDataAnalizer.IsBulkReply(line.FirstChar))
			{
				var length = _redisDataAnalizer.ConvertToInt(line.Line);
				//check nil reply
				var data = length == -1 ? null : await _redisStream.ReadBlockLine(length).ConfigureAwait(false);
				return RedisResponse.CreateBulk(data);
			}
			if (_redisDataAnalizer.IsMultiBulkReply(line.FirstChar))
			{
				var partsCount = _redisDataAnalizer.ConvertToInt(line.Line);
				
				var parts = new RedisResponse[partsCount];
				for (int i = 0; i < partsCount; i++)
					parts[i] = await ReadResponseAsync().ConfigureAwait(false);

				return RedisResponse.CreateMultiBulk(parts);
			}
			throw new RedisException("Invalid reply type");
		}
		public Task Flush()
		{
			return _redisStream.Flush();
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
	}
}
