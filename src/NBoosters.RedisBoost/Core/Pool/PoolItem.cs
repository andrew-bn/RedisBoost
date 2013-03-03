using System;

namespace NBoosters.RedisBoost.Core.Pool
{
	internal struct PoolItem
	{
		public PoolItem(IPooledRedisClient client)
		{
			Client = client;
			Timestamp = DateTime.Now;
			DestroyingException = null;
		}
		public PoolItem(IPooledRedisClient client, Exception ex)
		{
			Client = client;
			Timestamp = DateTime.Now;
			DestroyingException = ex;
		}
		public readonly DateTime Timestamp;
		public readonly IPooledRedisClient Client;
		public readonly Exception DestroyingException;
		public bool HasErrors { get { return DestroyingException != null; } }
	}
}