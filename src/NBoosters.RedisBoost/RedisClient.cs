using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NBoosters.RedisBoost.Core;
using NBoosters.RedisBoost.Core.Misk;
using NBoosters.RedisBoost.Core.Pool;

namespace NBoosters.RedisBoost
{
	public partial class RedisClient : IPrepareSupportRedisClient, IRedisSubscription
	{
		internal enum ClientState
		{
			None,
			Connect,
			Subscription,
			Disconnect,
			Quit,
			FatalError,
		}

		static readonly ObjectsPool<IRedisChannel> _redisChannelPool = new ObjectsPool<IRedisChannel>();

		private readonly IRedisChannel _redisChannel;
		private readonly IRedisDataAnalizer _redisDataAnalizer;
		private readonly RedisConnectionStringBuilder _connectionStringBuilder;
		public string ConnectionString { get; private set; }
		private volatile ClientState _state;
		ClientState IPrepareSupportRedisClient.State { get { return _state; } }

		#region factory

		public static IRedisClientsPool CreateClientsPool()
		{
			return new RedisClientsPool();
		}
		public static IRedisClientsPool CreateClientsPool(int inactivityTimeout)
		{
			return new RedisClientsPool(inactivityTimeout: inactivityTimeout);
		}

		public static IRedisClientsPool CreateClientsPool(int maxPoolsSize, int inactivityTimeout)
		{
			return new RedisClientsPool(maxPoolsSize, inactivityTimeout);
		}

		public static async Task<IRedisClient> ConnectAsync(EndPoint endPoint)
		{
			var result = new RedisClient(new RedisConnectionStringBuilder(endPoint));
			await ((IPrepareSupportRedisClient)result).PrepareClientConnection().ConfigureAwait(false);
			return result;
		}

		public static async Task<IRedisClient> ConnectAsync(EndPoint endPoint, int dbIndex)
		{
			var result = new RedisClient(new RedisConnectionStringBuilder(endPoint, dbIndex));
			await ((IPrepareSupportRedisClient)result).PrepareClientConnection().ConfigureAwait(false);
			return result;
		}

		public static async Task<IRedisClient> ConnectAsync(string connectionString)
		{
			var result = new RedisClient(new RedisConnectionStringBuilder(connectionString));
			await ((IPrepareSupportRedisClient)result).PrepareClientConnection().ConfigureAwait(false);
			return result;
		}

		async Task IPrepareSupportRedisClient.PrepareClientConnection()
		{
			await ConnectAsync().ConfigureAwait(false);
			var dbIndex = _connectionStringBuilder.DbIndex;
			if (dbIndex != 0)
				await SelectAsync(dbIndex).ConfigureAwait(false);
		}

		#endregion

		protected RedisClient(RedisConnectionStringBuilder connectionString)
		{
			ConnectionString = connectionString.ToString();
			_connectionStringBuilder = connectionString;

			var sock = new Socket(_connectionStringBuilder.EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			_redisChannel = _redisChannelPool.GetOrCreate(() =>
			{
				var dataAnalizer = new RedisDataAnalizer();
				return new RedisChannel(new RedisStream(dataAnalizer), dataAnalizer);
			});

			_redisChannel.EngageWith(sock);

			_redisDataAnalizer = _redisChannel.RedisDataAnalizer;
		}

		#region converters
		private byte[] ConvertToByteArray(Aggregation aggregation)
		{
			var aggr = RedisConstants.Sum;
			if (aggregation == Aggregation.Max)
				aggr = RedisConstants.Max;
			if (aggregation == Aggregation.Min)
				aggr = RedisConstants.Min;
			return aggr;
		}
		private byte[] ConvertToByteArray(string data)
		{
			return _redisDataAnalizer.ConvertToByteArray(data);
		}
		private byte[] ConvertToByteArray(int data)
		{
			return _redisDataAnalizer.ConvertToByteArray(data.ToString());
		}
		private byte[] ConvertToByteArray(double data)
		{
			byte[] result;
			if (double.IsPositiveInfinity(data))
				result = RedisConstants.PositiveInfinity;
			else if (double.IsNegativeInfinity(data))
				result = RedisConstants.NegativeInfinity;
			else
				result = _redisDataAnalizer.ConvertToByteArray(data.ToString("R", CultureInfo.InvariantCulture));

			return result;
		}

		private string ConvertToString(byte[] result)
		{
			return _redisDataAnalizer.ConvertToString(result, 0);
		}
		#endregion
		#region read response
		private async Task<byte[][]> MultiBulkResponseCommand(params byte[][] args)
		{
			var reply = await SendCommandAndReadResponse(args).ConfigureAwait(false);
			return ToBytesArray(reply.AsMultiBulk());
		}
		private async Task<string> StatusResponseCommand(params byte[][] args)
		{
			var reply = await SendCommandAndReadResponse(args).ConfigureAwait(false);
			if (reply.ResponseType != RedisResponseType.Status)
				return string.Empty;
			return reply.AsStatus();
		}
		private async Task<long> IntegerResponseCommand(params byte[][] args)
		{
			var reply = await SendCommandAndReadResponse(args).ConfigureAwait(false);
			if (reply.ResponseType != RedisResponseType.Integer)
				return default(long);
			return reply.AsInteger();
		}
		private async Task<long?> IntegerOrBulkNullResponseCommand(params byte[][] args)
		{
			var reply = await SendCommandAndReadResponse(args).ConfigureAwait(false);
			if (reply.ResponseType == RedisResponseType.Integer)
				return reply.AsInteger();
			return null;
		}
		private async Task<byte[]> BulkResponseCommand(params byte[][] args)
		{
			var reply = await SendCommandAndReadResponse(args).ConfigureAwait(false);
			return reply.ResponseType != RedisResponseType.Bulk ? null : reply.AsBulk();
		}
		private byte[][] ToBytesArray(RedisResponse[] response)
		{
			var result = new byte[response.Length][];
			for (int i = 0; i < result.Length; i++)
			{
				switch (response[i].ResponseType)
				{
					case RedisResponseType.Bulk:
						result[i] = response[i].AsBulk();
						break;
					case RedisResponseType.Integer:
						result[i] = ConvertToByteArray(response[i].AsInteger());
						break;
					case RedisResponseType.Status:
						result[i] = ConvertToByteArray(response[i].AsStatus());
						break;
				}
			}
			return result;
		}
		#endregion
		#region commands execution
		private async Task<RedisResponse> SendCommandAndReadResponse(params byte[][] args)
		{
			await ExecuteCommand(args).ConfigureAwait(false);
			return await ReadResponse().ConfigureAwait(false);
		}
		private async Task ExecuteCommand(params byte[][] args)
		{
			try
			{
				await _redisChannel.SendAsync(args).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				DisposeIfFatalError(ex);
				throw new RedisException(ex.Message, ex);
			}
		}
		public async Task<RedisResponse> ReadResponse()
		{
			try
			{
				var reply = await _redisChannel.ReadResponseAsync().ConfigureAwait(false);
				if (reply.ResponseType == RedisResponseType.Error)
					throw new RedisException(reply.AsError());
				return reply;
			}
			catch (RedisException)
			{
				throw;
			}
			catch (Exception ex)
			{
				DisposeIfFatalError(ex);
				throw new RedisException(ex.Message, ex);
			}
		}
		#endregion
		#region connection
		public async Task ConnectAsync()
		{
			try
			{
				await _redisChannel.ConnectAsync(_connectionStringBuilder.EndPoint).ConfigureAwait(false);
				_state = ClientState.Connect;
			}
			catch (Exception ex)
			{
				DisposeIfFatalError(ex);
				throw new RedisException(ex.Message, ex);
			}
		}
		public Task DisconnectAsync()
		{
			try
			{
				_state = ClientState.Disconnect;
				return _redisChannel.DisconnectAsync();
			}
			catch (Exception ex)
			{
				DisposeIfFatalError(ex);
				throw new RedisException(ex.Message, ex);
			}
		}

		#endregion

		private void DisposeIfFatalError(Exception ex)
		{
			if (ex is RedisException) return;
			_state = ClientState.FatalError;
			Dispose();
		}

		private int _disposed = 0;
		protected virtual void Dispose(bool disposing)
		{
			if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
					return;
			
			if (disposing)
			{
				try
				{
					_redisChannel.Dispose();
				}
				finally
				{
					_redisChannelPool.Release(_redisChannel);
				}
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}
	}
}
