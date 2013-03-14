using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBoosters.RedisBoost.Core.Misk
{
	public static class Extensions
	{
		public static Task ContinueWithIfNoError<T>(this T task, Action<T> action)
			where T:Task
		{
			return task.ContinueWith(t =>
				{
					if (task.IsFaulted)
						throw task.Exception.UnwrapAggregation();
					action(task);
				});
			
		}
		public static Task<TResult> ContinueWithIfNoError<T, TResult>(this T task, Func<T, TResult> func)
			where T : Task
		{
			return task.ContinueWith(t =>
			{
				if (task.IsFaulted)
					throw task.Exception.UnwrapAggregation();
				return func(task);
			});
		}
		public static Exception UnwrapAggregation(this Exception ex)
		{
			while (ex is AggregateException) ex = ex.InnerException;
			return ex;
		}
	}
}
