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
using System.Threading.Tasks;

namespace RedisBoost.Misk
{
	internal static class TaskExtensions
	{
		public static Task ContinueWithIfNoError<T,TResult>(this T task, TaskCompletionSource<TResult> tcs, Action<T> action)
			where T : Task
		{
			return task.ContinueWith(t =>
			{
				if (task.IsFaulted) 
					tcs.SetException(task.Exception);
				else
					action(task);
			});

		}
		public static Task ContinueWithIfNoError<T>(this T task, Action<T> action)
			where T:Task
		{
			return task.ContinueWith(t =>
				{
					if (task.IsFaulted) throw task.Exception.UnwrapAggregation();
					action(task);
				});
			
		}
		public static Task<TResult> ContinueWithIfNoError<T, TResult>(this T task, Func<T, TResult> func)
			where T : Task
		{
			return task.ContinueWith(t =>
			{
				if (task.IsFaulted) throw task.Exception.UnwrapAggregation();
				return func(task);
			});
		}
	}
}
