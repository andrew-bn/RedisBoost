#region Apache Licence, Version 2.0
/*
 Copyright 2013 Andrey Bulygin.

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
using RedisBoost.Core.Serialization;

namespace RedisBoost.Extensions.Queue
{
	internal sealed class RedisQueue<T>: ExtensionBase, IQueue<T>
	{
		private readonly string _queueName;

		public RedisQueue(string queueName, IRedisClientsPool pool, RedisConnectionStringBuilder connectionStringBuilder, BasicRedisSerializer serializer)
			:base(pool, connectionStringBuilder,serializer)
		{
			_queueName = queueName;
		}

		public int Count
		{
			get
			{
				return ExecuteFunc(() => {
											using (var cli = GetClient())
											{
												return (int) cli.LLenAsync(_queueName).Result;
											}
										 });
			}
		}

		public void Clear()
		{
			ExecuteFunc(() =>
				{
					using (var cli = GetClient())
					{
						return cli.DelAsync(_queueName);
					}
				});
		}

		public int Enqueue(T value)
		{
			return ExecuteFunc(() =>
				{
					using (var cli = GetClient())
					{
						return (int) cli.RPushAsync(_queueName, value).Result;
					}
				});
		}

		public T Dequeue()
		{
			T result;
			if (!TryDequeue(out result))
				throw new InvalidOperationException("The queue is empty");
			return result;
		}

		public bool TryDequeue(out T result)
		{
			var value = ExecuteFunc(() =>
				{
					using (var cli = GetClient())
					{
						return cli.LPopAsync(_queueName).Result;
					}
				});

			result = default(T);
			if (value.Value == null) return false;
			result = value.As<T>();
			return true;
		}

		public T Peek()
		{
			T result;
			if (!TryPeek(out result))
				throw new InvalidOperationException("The queue is empty");
			return result;
		}

		public bool TryPeek(out T result)
		{
			var value = ExecuteFunc(() =>
				{
					using (var cli = GetClient())
					{
						return cli.LRangeAsync(_queueName, 0, 0).Result;
					}
				});
			result = default(T);

			if (value.Length <= 0) return false;
			result = value[0].As<T>();
			return true;
		}
	}
}
