namespace NBoosters.RedisBoost.Core.Pool
{
	internal interface IPooledRedisClient: IPrepareSupportRedisClient
	{
		
		void Destroy();
	}
}
