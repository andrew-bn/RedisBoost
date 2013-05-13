using System;

namespace NBoosters.RedisBoost.Misk
{
	internal interface IBuffersPool
	{
		bool TryGet(out byte[] buffer);
		byte[] Get();
		void Release(byte[] buffer);
	}
}
