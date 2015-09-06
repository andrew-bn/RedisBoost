#region Apache Licence, Version 2.0
/*
 Copyright 2015 Andrey Bulygin.

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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using RedisBoost.Core;
using RedisBoost.Core.AsyncSocket;
using RedisBoost.Core.Channel;
using RedisBoost.Core.Pipeline;
using RedisBoost.Core.Pool;
using RedisBoost.Core.Receiver;
using RedisBoost.Core.Sender;
using RedisBoost.Core.Serialization;
using RedisBoost.Misk;

namespace RedisBoost
{
	public partial class RedisClient : IPrepareSupportRedisClient, IRedisSubscription
	{
		private static readonly BuffersPool _buffersPool;
		private static readonly BuffersPool _inputBuffersPool;
		static readonly ObjectsPool<IRedisChannel> _redisPipelinePool;
		
		static BasicRedisSerializer _defaultSerializer = new BasicRedisSerializer();
		public static BasicRedisSerializer DefaultSerializer
		{
			get { return _defaultSerializer; }
			set { _defaultSerializer = value; }
		}
		private static int _ioBufferSize = 1024 * 8;
		public static int IoBufferSize
		{
			get { return _ioBufferSize; }
			set
			{
				_ioBufferSize = value;
				_inputBuffersPool.BufferSize = value;
				_buffersPool.BufferSize = value;
			}
		}
		private static int _outputBuffersCount = 100;

		public static int OutputBuffersCount
		{
			get { return _outputBuffersCount; }
			set
			{
				_outputBuffersCount = value;
				_buffersPool.MaxPoolSize = value;
			}
		}
		private static int _inputBuffersCount = 50;

		public static int InputBuffersCount
		{
			get { return _inputBuffersCount; }
			set
			{
				_inputBuffersCount = value;
				_inputBuffersPool.MaxPoolSize = value;
			}
		}
		static RedisClient()
		{
			_buffersPool = new BuffersPool(IoBufferSize, OutputBuffersCount);
			_inputBuffersPool = new BuffersPool(IoBufferSize, InputBuffersCount);
			_redisPipelinePool = new ObjectsPool<IRedisChannel>();
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
			return ((IPrepareSupportRedisClient)result).PrepareClientConnection();
		}

		Task<IRedisClient> IPrepareSupportRedisClient.PrepareClientConnection()
		{
			var dbIndex = _connectionStringBuilder.DbIndex;
			var connectTask = ConnectAsync();
			return dbIndex == 0
						? connectTask.ContinueWithIfNoError(t => (IRedisClient)this)
						: connectTask.ContinueWithIfNoError(
										t => SelectAsync(dbIndex).ContinueWithIfNoError(
												tsk => (IRedisClient)this))
									  .Unwrap();
		}

		#endregion

		public string ConnectionString { get; private set; }
		public IRedisSerializer Serializer { get; private set; }

		private int _disposed;
		private readonly IRedisChannel _channel;
		private readonly RedisConnectionStringBuilder _connectionStringBuilder;

		ClientState IPrepareSupportRedisClient.State { get { return _channel.State; } }

		protected RedisClient(RedisConnectionStringBuilder connectionString, IRedisSerializer serializer)
		{
			ConnectionString = connectionString.ToString();
			_connectionStringBuilder = connectionString;
			Serializer = serializer ?? DefaultSerializer;

			var socket = new Socket(_connectionStringBuilder.EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			_channel = PrepareRedisChannel(socket);
		}

		private IRedisChannel PrepareRedisChannel(Socket socket)
		{
			var socketWrapper = new SocketWrapper(socket);

			var channel = _redisPipelinePool.GetOrCreate(() =>
				{
					var asyncSocket = new AsyncSocketWrapper();
					var pipeline = new RedisPipeline(asyncSocket, new RedisSender(_buffersPool, asyncSocket, false), new RedisReceiver(_inputBuffersPool, asyncSocket));
					return new RedisChannel(pipeline);
				});

			channel.ResetState();
			channel.EngageWith(socketWrapper);
			channel.EngageWith(Serializer);

			return channel;
		}

		public Task<RedisResponse> ExecuteAsync(string command, params object[] args)
		{
			var request = ComposeRequest(RedisConstants.SerializeCommandName(command), Serialize(args));
			return _channel.ExecuteRedisCommand(request);
		}

		#region read response
		private Task<MultiBulk> MultiBulkCommand(params byte[][] args)
		{
			return _channel.MultiBulkCommand(args);
		}

		private Task<string> StatusCommand(params byte[][] args)
		{
			return _channel.StatusCommand(args);
		}

		private Task<long> IntegerCommand(params byte[][] args)
		{
			return _channel.IntegerCommand(args);
		}

		private Task<long?> IntegerOrBulkNullCommand(params byte[][] args)
		{
			return _channel.IntegerOrBulkNullCommand(args);
		}

		private Task<Bulk> BulkCommand(params byte[][] args)
		{
			return _channel.BulkCommand(args);
		}

		private Task<RedisResponse> ExecuteRedisCommand(params byte[][] args)
		{
			return _channel.ExecuteRedisCommand(args);
		}
		#endregion

		#region commands execution

		private Task<RedisResponse> ReadDirectResponse()
		{
			return _channel.ReadDirectResponse();
		}

		private Task SendDirectRequest(params byte[][] args)
		{
			return _channel.SendDirectRequest(args);
		}
		#endregion

		#region connection
		public Task ConnectAsync()
		{
			return _channel.Connect(_connectionStringBuilder.EndPoint);
		}

		public Task DisconnectAsync()
		{
			return _channel.Disconnect();
		}

		#endregion

		#region disposing
		protected virtual void Dispose(bool disposing)
		{
			if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
				return;

			if (!disposing) return;

			_channel.Dispose();
			_redisPipelinePool.Release(_channel);
		}

		public void Dispose()
		{
			Dispose(true);
		}
		#endregion

		private void OneWayMode()
		{
			_channel.SwitchToOneWay();
		}

		private void SetQuitState()
		{
			_channel.SetQuitState();
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
				request[i * 2 + 2] = arg.KeyOrField.ToBytes();
				request[i * 2 + 3] = arg.IsArray ? (byte[])arg.Value : Serialize(arg.Value);
			}
			return request;
		}

		private byte[][] ComposeRequest(byte[] commandName, byte[] arg1, string[] args)
		{
			return ComposeRequest(commandName, arg1, args.Select(a => a.ToBytes()).ToArray());
		}

		private byte[][] ComposeRequest(byte[] commandName, byte[][] args)
		{
			var request = new byte[args.Length + 1][];
			request[0] = commandName;
			for (int i = 0; i < args.Length; i++)
				request[i + 1] = args[i];

			return request;
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
				request[i + 3] = args[i].ToBytes();

			return request;
		}
		private byte[][] ComposeRequest(byte[] commandName, string[] args, byte[] lastArg = null)
		{
			if (args.Length == 0)
				throw new ArgumentException("Invalid args count", "args");

			var request = new byte[args.Length + ((lastArg != null) ? 2 : 1)][];
			request[0] = commandName;

			for (int i = 0; i < args.Length; i++)
				request[i + 1] = args[i].ToBytes();

			if (lastArg != null)
				request[request.Length - 1] = lastArg;
			return request;
		}
		#endregion

		#region serialization
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
		#endregion

	}
}
