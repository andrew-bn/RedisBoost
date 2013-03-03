namespace NBoosters.RedisBoost.Core.Pool
{
	internal class PooledRedisClient : RedisClient, IPooledRedisClient
	{
		private readonly RedisClientsPool _pool;

		public PooledRedisClient(RedisClientsPool pool, RedisConnectionStringBuilder connectionString)
			: base(connectionString)
		{
			_pool = pool;
		}

		public void Destroy()
		{
			base.Dispose(true);
		}
		protected override void Dispose(bool disposing)
		{
			_pool.ReturnClient(this);
		}
	}
}