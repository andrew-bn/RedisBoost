using System;

namespace NBoosters.RedisBoost.Core.Pool
{
	internal struct PoolItem
	{
		public PoolItem(IPooledRedisClient client)
		{
			Client = client;
			Timestamp = DateTime.Now;
			DestroingException = null;
		}
		public PoolItem(IPooledRedisClient client, Exception ex)
		{
			Client = client;
			Timestamp = DateTime.Now;
			DestroingException = ex;
		}
		public readonly DateTime Timestamp;
		public readonly IPooledRedisClient Client;
		public readonly Exception DestroingException;
		public bool HasErrors { get { return DestroingException != null; } }
	}
}