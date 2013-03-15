using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NBoosters.RedisBoost.Core.Serialization;
using NBoosters.RedisBoost.Extensions.ManualResetEvent;
using NBoosters.RedisBoost.Extensions.Queue;

namespace NBoosters.RedisBoost.Extensions
{
	internal class ExtensionsFactory: IExtensionsFactory
	{
		private const string QUEUE_GUID = "F3A33B3BA52F4C759D9E4CC96E7768B7";
		private const string MRE_GUID = "184A3138C0734E17877608C47A0D874E";
		private readonly IRedisClientsPool _pool;
		private readonly RedisConnectionStringBuilder _connectionStringBuilder;
		private readonly BasicRedisSerializer _serializer;

		public ExtensionsFactory(IRedisClientsPool pool, RedisConnectionStringBuilder connectionStringBuilder, BasicRedisSerializer serializer)
		{
			_pool = pool;
			_connectionStringBuilder = connectionStringBuilder;
			_serializer = serializer;
		}

		public IQueue<T> CreateQueue<T>(string name)
		{
			return new RedisQueue<T>(name + QUEUE_GUID, _pool, _connectionStringBuilder, _serializer);
		}


		public IManualResetEvent CreateManualResetEvent(string name)
		{
			return new RedisManualResetEvent(name+MRE_GUID,_pool,null,_connectionStringBuilder,_serializer);
		}
	}
}
