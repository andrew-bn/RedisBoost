using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NBoosters.RedisBoost.Core.Pool
{
	internal sealed class RedisClientsPool : IRedisClientsPool, IDisposable
	{
		private const int INACTIVITY_TIMEOUT = 1000 * 60; // 1 min
		private const int DESTROY_TIMEOUT = 10 * 1000;// 10 sec
		private const int MAX_POOL_SIZE = 100;

		private readonly int _maxPoolSize;
		private readonly int _inactivityTimeout;
		private readonly int _destroyTimeout;
		private readonly Func<RedisConnectionStringBuilder, IPooledRedisClient> _redisClientsFactory;
		private readonly Timer _timer;

		public RedisClientsPool(int maxPoolSize = MAX_POOL_SIZE, 
								int inactivityTimeout = INACTIVITY_TIMEOUT, 
								int destroyTimeout = DESTROY_TIMEOUT)
		{
			_maxPoolSize = maxPoolSize;
			_inactivityTimeout = inactivityTimeout;
			_destroyTimeout = destroyTimeout;
			_timer = new Timer(TimerCallback, null, inactivityTimeout, int.MaxValue);
			_redisClientsFactory = sb => new PooledRedisClient(this, sb);
		}
		internal RedisClientsPool(int maxPoolSize, int inactivityTimeout, int destroyTimeout, Func<RedisConnectionStringBuilder, IPooledRedisClient> redisClientsFactory)
			: this(maxPoolSize, inactivityTimeout, destroyTimeout)
		{
			_redisClientsFactory = redisClientsFactory;
		}
		private readonly ConcurrentDictionary<string, ConcurrentQueue<PoolItem>> _pools = new ConcurrentDictionary<string, ConcurrentQueue<PoolItem>>();

		public Task<IRedisClient> CreateClientAsync(string connectionString)
		{
			return CreateClientAsync(new RedisConnectionStringBuilder(connectionString));
		}

		public Task<IRedisClient> CreateClientAsync(EndPoint endPoint)
		{
			return CreateClientAsync(new RedisConnectionStringBuilder(endPoint));
		}

		public Task<IRedisClient> CreateClientAsync(EndPoint endPoint, int dbIndex)
		{
			return CreateClientAsync(new RedisConnectionStringBuilder(endPoint, dbIndex));
		}

		internal Task<IRedisClient> CreateClientAsync(RedisConnectionStringBuilder connectionString)
		{
			ThrowIfDisposed();

			var tcs = new TaskCompletionSource<IRedisClient>();

			var pool = _pools.GetOrAdd(connectionString.ToString(), k => new ConcurrentQueue<PoolItem>());

			PoolItem item;
			if (pool.TryDequeue(out item))
			{
				if (item.HasErrors)
					tcs.SetException(item.DestroingException);
				else
					tcs.SetResult(item.Client);
			}
			else
			{
				CreateAndPrepareClient(connectionString)
					.ContinueWith(t =>
						{
							if (t.IsFaulted)
								tcs.SetException(t.Exception);
							else tcs.SetResult(t.Result);
						});
			}
			return tcs.Task;
		}

		private async Task<IRedisClient> CreateAndPrepareClient(RedisConnectionStringBuilder connectionString)
		{
			var client = _redisClientsFactory(connectionString);
			await client.PrepareClientConnection().ConfigureAwait(false);
			return client;
		}

		internal void ReturnClient(IPooledRedisClient pooledRedisClient)
		{
			if (pooledRedisClient.State == RedisClient.ClientState.Subscription ||
				pooledRedisClient.State == RedisClient.ClientState.Quit ||
				pooledRedisClient.State == RedisClient.ClientState.Disconnect)
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
					{
						var ex = DestroyPoolItem(item);
						if (ex != null)
							queue.Enqueue(new PoolItem(item.Client, ex));
					}
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
			if (item.HasErrors) return item.DestroingException;
			return TryDestroyClient(item.Client);
		}
		private Exception TryDestroyClient(IPooledRedisClient client)
		{
			try
			{
				client.QuitAsync().Wait(_destroyTimeout);
				client.Destroy();
				return null;
			}
			catch (Exception ex)
			{
				return ex;
			}
		}

		private int _disposed = 0;
		public void Dispose()
		{
			if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
				return;

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
