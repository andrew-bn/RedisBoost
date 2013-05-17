using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace NBoosters.RedisBoost.Misk
{
	internal class BuffersPool: IBuffersPool
	{
		public int BufferSize { get; set; }
		private readonly ObjectsPool<byte[]> _pool = new ObjectsPool<byte[]>();
		private readonly ConcurrentQueue<Action<byte[]>> _callbacks = new ConcurrentQueue<Action<byte[]>>(); 
		private int _poolSize;

		public int MaxPoolSize { get; set; }

		public BuffersPool(int defaultBufferSize, int defaultMaxPoolSize)
		{
			BufferSize = defaultBufferSize;
			MaxPoolSize = defaultMaxPoolSize;
		}

		public bool TryGet(out byte[] buffer, Action<byte[]> asyncBufferGetCallBack )
		{
			var sync = TryGet(out buffer);
			if (!sync)
			{
				_callbacks.Enqueue(asyncBufferGetCallBack);
				if (TryGet(out buffer))
					Release(buffer);
			}

			return sync;
		}

		private bool TryGet(out byte[] buffer)
		{
			buffer = _pool.GetOrCreate(() => 
				(Interlocked.Increment(ref _poolSize) > MaxPoolSize) ? null : new byte[BufferSize]);
			return buffer != null;
		}

		public void Release(byte[] buffer)
		{
			Action<byte[]> callBack;
			if (_callbacks.TryDequeue(out callBack))
				callBack(buffer);
			else _pool.Release(buffer);
		}
		public void ReleaseWithoutNotification(byte[] buffer)
		{
			_pool.Release(buffer);
		}
		public void NotifyWaiters()
		{
			while (true)
			{
				Action<byte[]> callback;
				if (!_callbacks.TryDequeue(out callback))
					return;
				byte[] tempBuffer;
				if (!TryGet(out tempBuffer,callback))
					return;
				callback(tempBuffer);
			}
		}
	}
}
