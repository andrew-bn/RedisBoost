using System;

namespace NBoosters.RedisBoost.Core.Pool
{
	internal struct PoolItem
	{
		public PoolItem(IPooledRedisClient client)
		{
			Client = client;
			Timestamp = DateTime.Now;
		}

		public readonly DateTime Timestamp;
		public readonly IPooledRedisClient Client;
	}
}