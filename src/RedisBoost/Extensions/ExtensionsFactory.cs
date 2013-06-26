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

using RedisBoost.Core.Serialization;
using RedisBoost.Extensions.ManualResetEvent;
using RedisBoost.Extensions.Queue;

namespace RedisBoost.Extensions
{
	internal class ExtensionsFactory: IExtensionsFactory
	{
		private const string QUEUE_GUID = "F3A33B3BA52F4C759D9E4CC96E7768B7";
		private const string MRE_GUID = "184A3138C0734E17877608C47A0D874E";
		private readonly IRedisClientsPool _pool;
		private readonly RedisConnectionStringBuilder _connectionStringBuilder;
		private readonly BasicRedisSerializer _serializer;
		private static IPubSubClientsPool _pubSubPool = new PubSubClientsPool();

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
			return new RedisManualResetEvent(name + MRE_GUID, _pool, _pubSubPool, _connectionStringBuilder, _serializer);
		}
	}
}
