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
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using RedisBoost.Misk;
using RedisBoost.Core.Serialization;

namespace RedisBoost.Core.Pool
{
	internal class RedisClientsPool : IRedisClientsPool
	{
		private const int INACTIVITY_TIMEOUT = 1000 * 60; // 1 min
		private const int DESTROY_TIMEOUT = 10 * 1000;// 10 sec
		private const int MAX_POOL_SIZE = 100;

		private readonly int _maxPoolSize;
		private readonly int _inactivityTimeout;
		private readonly int _destroyTimeout;
		private readonly Func<RedisConnectionStringBuilder, BasicRedisSerializer, IPooledRedisClient> _redisClientsFactory;
		private readonly Timer _timer;

		public RedisClientsPool(int maxPoolSize = MAX_POOL_SIZE, 
								int inactivityTimeout = INACTIVITY_TIMEOUT, 
								int destroyTimeout = DESTROY_TIMEOUT)
		{
			_maxPoolSize = maxPoolSize;
			_inactivityTimeout = inactivityTimeout;
			_destroyTimeout = destroyTimeout;
			_timer = new Timer(TimerCallback, null, inactivityTimeout, int.MaxValue);
			_redisClientsFactory = (sb,s)=> new PooledRedisClient(this, sb, s);
		}
		internal RedisClientsPool(int maxPoolSize, int inactivityTimeout, int destroyTimeout, Func<RedisConnectionStringBuilder,BasicRedisSerializer, IPooledRedisClient> redisClientsFactory)
			: this(maxPoolSize, inactivityTimeout, destroyTimeout)
		{
			_redisClientsFactory = redisClientsFactory;
		}
		private readonly ConcurrentDictionary<string, ConcurrentQueue<PoolItem>> _pools = new ConcurrentDictionary<string, ConcurrentQueue<PoolItem>>();

		public Task<IRedisClient> CreateClientAsync(string connectionString, BasicRedisSerializer serializer = null)
		{
			return CreateClientAsync(new RedisConnectionStringBuilder(connectionString),serializer);
		}

		public Task<IRedisClient> CreateClientAsync(EndPoint endPoint, int dbIndex = 0, BasicRedisSerializer serializer = null)
		{
			return CreateClientAsync(new RedisConnectionStringBuilder(endPoint, dbIndex),serializer);
		}

		public Task<IRedisClient> CreateClientAsync(string host, int port, int dbIndex = 0, BasicRedisSerializer serializer = null)
		{
			return CreateClientAsync(new RedisConnectionStringBuilder(host, port, dbIndex),serializer);
		}

		public Task<IRedisClient> CreateClientAsync(RedisConnectionStringBuilder connectionString, BasicRedisSerializer serializer = null)
		{
			ThrowIfDisposed();

			var tcs = new TaskCompletionSource<IRedisClient>();

			var pool = _pools.GetOrAdd(connectionString.ToString(), k => new ConcurrentQueue<PoolItem>());

			PoolItem item;
			if (pool.TryDequeue(out item))
				tcs.SetResult(item.Client);
			else
			{
				CreateAndPrepareClient(connectionString, serializer)
					.ContinueWith(t =>
						{
							if (t.IsFaulted)
								tcs.SetException(t.Exception.UnwrapAggregation());
							else tcs.SetResult(t.Result);
						});
			}
			return tcs.Task;
		}

		private Task<IRedisClient> CreateAndPrepareClient(RedisConnectionStringBuilder connectionString, BasicRedisSerializer serializer)
		{
			var client = _redisClientsFactory(connectionString, serializer);
			return client.PrepareClientConnection();
		}

		internal void ReturnClient(IPooledRedisClient pooledRedisClient)
		{
			if (DestroyClientCondition(pooledRedisClient))
			{
				pooledRedisClient.Destroy();
			}
			else if (Disposed)
			{
				TryDestroyClient(pooledRedisClient);
			}
			else
			{
				var pool = _pools.GetOrAdd(pooledRedisClient.ConnectionString, k => new ConcurrentQueue<PoolItem>());
				if (PoolIsOversized(pool))
					TryDestroyClient(pooledRedisClient);
				else
					pool.Enqueue(new PoolItem(pooledRedisClient));
			}
		}

		protected virtual bool DestroyClientCondition(IPooledRedisClient pooledRedisClient)
		{
			return pooledRedisClient.State == ClientState.Subscription ||
			       pooledRedisClient.State == ClientState.Quit ||
			       pooledRedisClient.State == ClientState.Disconnect ||
			       pooledRedisClient.State == ClientState.FatalError;
		}

		private bool PoolIsOversized(ConcurrentQueue<PoolItem> pool)
		{
			return pool.Count >= _maxPoolSize;
		}

		private void TimerCallback(object state)
		{
			if (Disposed)
				return;

			_timer.Change(int.MaxValue, int.MaxValue);
			try
			{
				FreeNotUsedClients();
			}
			finally
			{
				_timer.Change(_inactivityTimeout, int.MaxValue);
			}
		}

		private void FreeNotUsedClients()
		{
			var now = DateTime.Now;
			foreach (var pool in _pools)
			{
				while (true)
				{
					var queue = pool.Value;
					PoolItem item;
					if (!queue.TryDequeue(out item))
						break;

					if ((now - item.Timestamp).TotalMilliseconds > _inactivityTimeout)
						DestroyPoolItem(item);
					else
					{
						queue.Enqueue(item);
						break;
					}
				}
			}
		}

		private Exception DestroyPoolItem(PoolItem item)
		{
			return TryDestroyClient(item.Client);
		}
		private Exception TryDestroyClient(IPooledRedisClient client)
		{
			try
			{
				DestroyClient(client);
				return null;
			}
			catch (Exception ex)
			{
				return ex;
			}
		}
		protected virtual void DestroyClient(IPooledRedisClient client)
		{
			client.QuitAsync().Wait(_destroyTimeout);
			client.Destroy();
		}
		protected virtual void Dispose(bool disposing)
		{
			if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
				return;

			if (disposing)
			{
				_timer.Dispose();

				foreach (var pool in _pools)
				{
					var queue = pool.Value;
					while (queue.Count != 0)
					{
						PoolItem item;
						if (queue.TryDequeue(out item))
							DestroyPoolItem(item);
					}
				}
			}
		}
		private int _disposed;
		public void Dispose()
		{
			Dispose(true);
		}
		private bool Disposed
		{
			get { return _disposed != 0; }
		}
		private void ThrowIfDisposed()
		{
			if (Disposed)
				throw new ObjectDisposedException("RedisClientsPool");
		}
	}
}
