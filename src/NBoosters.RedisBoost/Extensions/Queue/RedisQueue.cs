using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NBoosters.RedisBoost.Core.Misk;
using NBoosters.RedisBoost.Core.Serialization;

namespace NBoosters.RedisBoost.Extensions.Queue
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
