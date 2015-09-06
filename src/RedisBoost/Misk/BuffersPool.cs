#region Apache Licence, Version 2.0
/*
 Copyright 2015 Andrey Bulygin.

 Licensed under the Apache License, Version 2.0 (the "License"); 
 you may not use this file except in compliance with the License. 
 You may obtain a copy of the License at 

		http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software 
 distributed under the License is distributed on an "AS IS" BASIS, 
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
 See the License for the specific language governing permissions 
 and limitations under the License.
 */
#endregion

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace RedisBoost.Misk
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
