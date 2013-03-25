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
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NBoosters.RedisBoost.Core;
using NBoosters.RedisBoost.Core.Pipeline;
using NBoosters.RedisBoost.Core.Pool;
using NBoosters.RedisBoost.Core.RedisChannel;
using NBoosters.RedisBoost.Core.RedisStream;
using NBoosters.RedisBoost.Core.Serialization;
using NBoosters.RedisBoost.Misk;

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
		#region request composers
		private byte[][] ComposeRequest(byte[] commandName, byte[] arg1, MSetArgs[] args)
		{
			if (args.Length == 0)
				throw new ArgumentException("Invalid args count", "args");

			var request = new byte[args.Length * 2 + 2][];
			request[0] = commandName;
			request[1] = arg1;
			for (int i = 0; i < args.Length; i++)
			{
				var arg = args[i];
				request[i * 2 + 2] = ConvertToByteArray(arg.KeyOrField);
				request[i * 2 + 3] = arg.IsArray ? (byte[])arg.Value : Serialize(arg.Value);
			}
			return request;
		}

		private byte[][] ComposeRequest(byte[] commandName, byte[] arg1, string[] args)
		{
			return ComposeRequest(commandName, arg1, args.Select(ConvertToByteArray).ToArray());
		}

		private byte[][] ComposeRequest(byte[] commandName, byte[] arg1, byte[][] args)
		{
			if (args.Length == 0)
				throw new ArgumentException("Invalid args count", "args");

			var request = new byte[args.Length + 2][];
			request[0] = commandName;
			request[1] = arg1;
			for (int i = 0; i < args.Length; i++)
				request[i + 2] = args[i];

			return request;
		}
		private byte[][] ComposeRequest(byte[] commandName, byte[] arg1, byte[] arg2, string[] args)
		{
			if (args.Length == 0)
				throw new ArgumentException("Invalid args count", "args");

			var request = new byte[args.Length + 3][];
			request[0] = commandName;
			request[1] = arg1;
			request[2] = arg2;

			for (int i = 0; i < args.Length; i++)
				request[i + 3] = ConvertToByteArray(args[i]);

			return request;
		}
		private byte[][] ComposeRequest(byte[] commandName, string[] args, byte[] lastArg = null)
		{
			if (args.Length == 0)
				throw new ArgumentException("Invalid args count", "args");

			var request = new byte[args.Length + ((lastArg!=null)?2:1)][];
			request[0] = commandName;

			for (int i = 0; i < args.Length; i++)
				request[i + 1] = ConvertToByteArray(args[i]);

			if (lastArg != null)
				request[request.Length - 1] = lastArg;
			return request;
		}
		#endregion
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

		private Task SendDirectRequest(params byte[][] args)
		{
			var tcs = new TaskCompletionSource<bool>();

			var channelArgs = new ChannelAsyncEventArgs();
			channelArgs.SendData = args;
			channelArgs.Completed = SendDirectCallBack;
			channelArgs.UserToken = tcs;

			if (!_redisChannel.SendAsync(channelArgs))
				SendDirectCallBack(channelArgs);

			return tcs.Task;
		}
		private void SendDirectCallBack(ChannelAsyncEventArgs args)
		{
			if (!_redisChannel.BufferIsEmpty)
			{
				args.Completed = a =>
				{
					if (a.HasException)
					{
						var err = a.Exception;
						DisposeIfFatalError(err);
						if (!(a.Exception is RedisException))
							err = new RedisException(err.Message, err);

						((TaskCompletionSource<bool>)args.UserToken).SetException(err);
					}
					else ((TaskCompletionSource<bool>)args.UserToken).SetResult(true);
				};

				if (!_redisChannel.Flush(args))
					args.Completed(args);
			}
		}
		public Task<RedisResponse> ReadDirectResponse()
		{
			var tcs = new TaskCompletionSource<RedisResponse>();
			var args = new ChannelAsyncEventArgs();
			args.Completed = e => ProcessRedisResponse(tcs,e.Exception,e.RedisResponse);
			if (!_redisChannel.ReadResponseAsync(args))
				args.Completed(args);
			return tcs.Task;
		}

		#endregion
		#region connection
		public Task ConnectAsync()
		{
			var tcs = new TaskCompletionSource<bool>();
			_redisChannel.ConnectAsync(_connectionStringBuilder.EndPoint,
			    (snk,ex) =>
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
					    return snk;
				    });
			return tcs.Task;
		}
		public Task DisconnectAsync()
		{
			var tcs = new TaskCompletionSource<bool>();
			_redisChannel.DisconnectAsync((s,ex) =>
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
					return s;
				});
			return tcs.Task;
		}

		#endregion
		#region disposing
		private void DisposeIfFatalError(Exception ex)
		{
			if (ex is RedisException) return;
			_state = ClientState.FatalError;
			Dispose();
		}

		private int _disposed;
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
		#endregion
	}
}
