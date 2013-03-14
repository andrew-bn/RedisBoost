using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NBoosters.RedisBoost.Core;
using NBoosters.RedisBoost.Core.Misk;
using NBoosters.RedisBoost.Core.Pipeline;
using NBoosters.RedisBoost.Core.Pool;
using NBoosters.RedisBoost.Core.Serialization;

namespace NBoosters.RedisBoost
{
	public partial class RedisClient : IPrepareSupportRedisClient, IRedisSubscription
	{
		static BasicRedisSerializer _defaultSerializer = new BasicRedisSerializer();
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

		private readonly IRedisPipeline _redisPipeline;
		private readonly IRedisChannel _redisChannel;
		private readonly IRedisDataAnalizer _redisDataAnalizer;
		private readonly RedisConnectionStringBuilder _connectionStringBuilder;
		public string ConnectionString { get; private set; }
		private volatile ClientState _state;
		ClientState IPrepareSupportRedisClient.State { get { return _state; } }
		public IRedisSerializer Serializer { get; private set; }
		public static BasicRedisSerializer DefaultSerializer
		{
			get { return _defaultSerializer; }
			set { _defaultSerializer = value; }
		}
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

		public static Task<IRedisClient> ConnectAsync(EndPoint endPoint, int dbIndex = 0, BasicRedisSerializer serializer = null)
		{
			var result = new RedisClient(new RedisConnectionStringBuilder(endPoint, dbIndex), serializer);
			return ((IPrepareSupportRedisClient)result).PrepareClientConnection();
		}

		public static Task<IRedisClient> ConnectAsync(string host, int port, int dbIndex = 0, BasicRedisSerializer serializer = null)
		{
			var result = new RedisClient(new RedisConnectionStringBuilder(host, port, dbIndex), serializer);
			return ((IPrepareSupportRedisClient)result).PrepareClientConnection();
		}

		public static Task<IRedisClient> ConnectAsync(string connectionString, BasicRedisSerializer serializer = null)
		{
			var result = new RedisClient(new RedisConnectionStringBuilder(connectionString), serializer);
			return ((IPrepareSupportRedisClient) result).PrepareClientConnection();
		}

		Task<IRedisClient> IPrepareSupportRedisClient.PrepareClientConnection()
		{
			var dbIndex = _connectionStringBuilder.DbIndex;
			var connectTask = ConnectAsync();
			return dbIndex == 0 
						? connectTask.ContinueWithIfNoError(t=>(IRedisClient)this)
						: connectTask.ContinueWithIfNoError(
										t =>SelectAsync(dbIndex).ContinueWithIfNoError(
												tsk =>(IRedisClient)this))
									  .Unwrap();
		}

		#endregion

		protected RedisClient(RedisConnectionStringBuilder connectionString, IRedisSerializer serializer)
		{
			ConnectionString = connectionString.ToString();
			_connectionStringBuilder = connectionString;
			Serializer = serializer ?? DefaultSerializer;

			var sock = new Socket(_connectionStringBuilder.EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			_redisChannel = _redisChannelPool.GetOrCreate(() =>
			{
				var dataAnalizer = new RedisDataAnalizer();
				return new RedisChannel(new RedisStream(dataAnalizer), dataAnalizer);
			});

			_redisChannel.EngageWith(sock, Serializer);

			_redisDataAnalizer = _redisChannel.RedisDataAnalizer;

			_redisPipeline = new RedisPipeline(_redisChannel);

		}

		#region converters

		private byte[] Serialize<T>(T value)
		{
			return Serializer.Serialize(value);
		}

		private byte[][] Serialize<T>(T[] values)
		{
			var result = new byte[values.Length][];
			for (int i = 0; i < result.Length; i++)
				result[i] = Serializer.Serialize(values[i]);
			return result;
		}
		private T Deserialize<T>(byte[] value)
		{
			return (T)Serializer.Deserialize(typeof(T), value);
		}
		private static byte[] ConvertToByteArray(BitOpType bitOp)
		{
			var result = RedisConstants.And;
			if (bitOp == BitOpType.Not)
				result = RedisConstants.Not;
			else if (bitOp == BitOpType.Or)
				result = RedisConstants.Or;
			else if (bitOp == BitOpType.Xor)
				result = RedisConstants.Xor;

			return result;
		}
		private static byte[] ConvertToByteArray(Subcommand subcommand)
		{
			var result = RedisConstants.RefCount;
			if (subcommand == Subcommand.IdleTime)
				result = RedisConstants.IdleTime;
			else if (subcommand == Subcommand.Encoding)
				result = RedisConstants.ObjEncoding;

			return result;
		}
		private static byte[] ConvertToByteArray(Aggregation aggregation)
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
		private byte[] ConvertToByteArray(long data)
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
		private Task<MultiBulk> MultiBulkResponseCommand(params byte[][] args)
		{
			return ExecutePipelinedCommand(args).ContinueWithIfNoError(t=>t.Result.AsMultiBulk());
		}
		private Task<string> StatusResponseCommand(params byte[][] args)
		{
			return ExecutePipelinedCommand(args).ContinueWithIfNoError(
				t =>
					{
						var reply = t.Result;
						if (reply.ResponseType != RedisResponseType.Status)
							return string.Empty;
						return reply.AsStatus();
					});
		}
		private Task<long> IntegerResponseCommand(params byte[][] args)
		{
			return ExecutePipelinedCommand(args).ContinueWithIfNoError(t =>
				{
					var reply = t.Result;
					if (reply.ResponseType != RedisResponseType.Integer)
						return default(long);
					return reply.AsInteger();
				});
			
		}
		private Task<long?> IntegerOrBulkNullResponseCommand(params byte[][] args)
		{
			return ExecutePipelinedCommand(args).ContinueWithIfNoError(
				t =>
					{
						var reply = t.Result;
						if (reply.ResponseType == RedisResponseType.Integer)
							return reply.AsInteger();
						return (long?)null;
					});

		}
		private Task<Bulk> BulkResponseCommand(params byte[][] args)
		{
			return ExecutePipelinedCommand(args).ContinueWithIfNoError(
				t =>
					{
						var reply = t.Result;
						return reply.ResponseType != RedisResponseType.Bulk ? null : reply.AsBulk();
					});
			
		}

		#endregion
		#region commands execution
		private void ClosePipeline()
		{
			_redisPipeline.ClosePipeline();
		}
		private void ProcessRedisResponse(TaskCompletionSource<RedisResponse> tcs, Exception ex, RedisResponse response)
		{
			if (ex != null)
			{
				DisposeIfFatalError(ex);
				if (!(ex is RedisException))
					ex = new RedisException(ex.Message, ex);
				tcs.SetException(ex);
			}
			else if (response.ResponseType == RedisResponseType.Error)
				tcs.SetException(new RedisException(response.AsError()));
			else
				tcs.SetResult(response);
		}
		private Task<RedisResponse> ExecutePipelinedCommand(params byte[][] args)
		{
			var tcs = new TaskCompletionSource<RedisResponse>();
			_redisPipeline.ExecuteCommandAsync(args, (ex, r) => ProcessRedisResponse(tcs,ex,r));
			return tcs.Task;
		}

		private Task SendDirectReqeust(params byte[][] args)
		{
			var tcs = new TaskCompletionSource<bool>();
			_redisChannel.SendAsync(args,
				ex =>
				{
					if (!_redisChannel.BufferIsEmpty)
						_redisChannel.Flush(err =>
							{
								if (err != null)
								{
									DisposeIfFatalError(ex);
									if (!(ex is RedisException))
										err = new RedisException(ex.Message, ex);
									tcs.SetException(err);
								}
								else tcs.SetResult(true);
							});
				});

			return tcs.Task;
		}
		public Task<RedisResponse> ReadDirectResponse()
		{
			var tcs = new TaskCompletionSource<RedisResponse>();
			_redisChannel.ReadResponseAsync((ex, r) => ProcessRedisResponse(tcs,ex,r));
			return tcs.Task;
		}

		#endregion
		#region connection
		public Task ConnectAsync()
		{
			var tcs = new TaskCompletionSource<bool>();
			_redisChannel.ConnectAsync(_connectionStringBuilder.EndPoint,
			    ex =>
				    {
					   if (ex != null)
					   {
						   DisposeIfFatalError(ex);
						   tcs.SetException(new RedisException(ex.Message, ex));
					   }
					   else
					   {
						   tcs.SetResult(true);
						   _state = ClientState.Connect;
					   }
				    });
			return tcs.Task;
		}
		public Task DisconnectAsync()
		{
			var tcs = new TaskCompletionSource<bool>();
			_redisChannel.DisconnectAsync(ex =>
				{
					if (ex != null)
					{
						DisposeIfFatalError(ex);
						tcs.SetException(new RedisException(ex.Message, ex));
					}
					else
					{
						_state = ClientState.Disconnect;
						tcs.SetResult(true);
					}
				});
			return tcs.Task;
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
