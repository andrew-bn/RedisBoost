using System;
using System.Collections.Generic;

namespace NBoosters.RedisBoost.Misk
{
	internal interface IBuffersPool
	{
		bool TryGet(out byte[] buffer, Action<byte[]> asyncBufferGetCallBack);
		void Release(byte[] buffer);
		void ReleaseWithoutNotification(byte[] buffer);
		void NotifyWaiters();
	}
}
