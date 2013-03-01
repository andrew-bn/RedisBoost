using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NBooster.RedisBoost
{
	internal sealed class RedisClientsPool : IRedisClientsPool, IDisposable
	{
		private class PooledRedisClient: RedisClient
		{
			private readonly RedisClientsPool _pool;

			public PooledRedisClient(RedisClientsPool pool, RedisConnectionStringBuilder connectionString)
				: base(connectionString)
			{
				_pool = pool;
			}
			public Task PrepareConnection()
			{
				return PrepareClientConnection();
			}
			public void Destroy()
			{
				base.Dispose(true);
			}
			protected override void Dispose(bool disposing)
			{
				if (State == ClientState.Subscription ||
					State == ClientState.Quit ||
				    State == ClientState.Disconnect)
				{
					base.Dispose(disposing);
				}
				else _pool.ReturnClient(this);
			}
		}

		private readonly int _timeout;
		private const int CONNECTION_TIMEOUT = 1000*60;
		private struct PoolItem
		{
			public PoolItem(PooledRedisClient client)
			{
				Client = client;
				Timestamp = DateTime.Now;
				DestroingException = null;
			}
			public PoolItem(PooledRedisClient client, Exception ex)
			{
				Client = client;
				Timestamp = DateTime.Now;
				DestroingException = ex;
			}
			public readonly DateTime Timestamp;
			public readonly PooledRedisClient Client;
			public readonly Exception DestroingException;
			public bool HasErrors { get { return DestroingException!=null; } }
		}

		private readonly Timer _timer;
		public RedisClientsPool()
			:this(CONNECTION_TIMEOUT)
		{
		}
		public RedisClientsPool(int timeout)
		{
			_timeout = timeout;
			_timer = new Timer(TimerCallback,null, timeout, timeout);
		}

		private readonly ConcurrentDictionary<string, ConcurrentQueue<PoolItem>> _pools = new ConcurrentDictionary<string, ConcurrentQueue<PoolItem>>();

		private void ReturnClient(PooledRedisClient pooledRedisClient)
		{
			var pool = _pools.GetOrAdd(pooledRedisClient.ConnectionString, k => new ConcurrentQueue<PoolItem>());
			pool.Enqueue(new PoolItem(pooledRedisClient));
		}

		public Task<IRedisClient> CreateClient(string connectionString)
		{
			return CreateClient(new RedisConnectionStringBuilder(connectionString));
		}

		public Task<IRedisClient> CreateClient(EndPoint endPoint)
		{
			return CreateClient(new RedisConnectionStringBuilder(endPoint));
		}
		
		public Task<IRedisClient> CreateClient(EndPoint endPoint, int dbIndex)
		{
			return CreateClient(new RedisConnectionStringBuilder(endPoint, dbIndex));
		}

		private Task<IRedisClient> CreateClient(RedisConnectionStringBuilder connectionString)
		{
			var tcs = new TaskCompletionSource<IRedisClient>();

			var pool = _pools.GetOrAdd(connectionString.ToString(), 
					k => new ConcurrentQueue<PoolItem>());

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

		private async Task<PooledRedisClient> CreateAndPrepareClient(RedisConnectionStringBuilder connectionString)
		{
			var client = new PooledRedisClient(this, connectionString);
			await client.PrepareConnection().ConfigureAwait(false);
			return client;
		}

		private void TimerCallback(object state)
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

					if ((now - item.Timestamp).TotalSeconds > _timeout)
					{
						var ex = TryDestroyUnusedClient(item);
						if (ex!=null)
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

		private static Exception TryDestroyUnusedClient(PoolItem item)
		{
			try
			{
				if (item.HasErrors) return item.DestroingException;
				item.Client.Destroy();
				return null;
			}
			catch (Exception ex)
			{
				return ex;
			}
		}
		public void Dispose()
		{
			_timer.Dispose();

			foreach (var pool in _pools)
			{
				var queue = pool.Value;
				while (queue.Count != 0)
				{
					PoolItem item;
					if (queue.TryDequeue(out item))
						TryDestroyUnusedClient(item);
				}
			}
		}
	}
}
