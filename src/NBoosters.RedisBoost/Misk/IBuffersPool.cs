namespace NBoosters.RedisBoost.Misk
{
	internal interface IBuffersPool
	{
		bool TryGet(out byte[] buffer);
		void Release(byte[] buffer);
	}
}
