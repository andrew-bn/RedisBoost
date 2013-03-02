using System;
using System.Collections.Concurrent;

namespace NBoosters.RedisBoost.Core.Misk
{
	internal class ObjectsPool<T> : IObjectsPool<T>
	{
		private readonly ConcurrentStack<T> _queue = new ConcurrentStack<T>();
		public T GetOrCreate(Func<T> factory)
		{
			T result;
			if (!_queue.TryPop(out result))
				result = factory();

			return result;
		}
		public void Release(T obj)
		{
			_queue.Push(obj);
		}
	}
}
