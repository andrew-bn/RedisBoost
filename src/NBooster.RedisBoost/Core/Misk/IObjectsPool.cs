using System;

namespace NBooster.RedisBoost.Core.Misk
{
	internal interface IObjectsPool<T>
	{
		T GetOrCreate(Func<T> factory);
		void Release(T obj);
	}
}