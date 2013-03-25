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
using NBoosters.RedisBoost.Core;
using NBoosters.RedisBoost.Core.Pool;
using NBoosters.RedisBoost.Core.Serialization;

namespace NBoosters.RedisBoost.Extensions
{
	internal class PubSubClientsPool : IPubSubClientsPool
	{
		private class PubSubPool : RedisClientsPool
		{
			protected override void DestroyClient(IPooledRedisClient client)
			{
				((IRedisSubscription)client).QuitAsync().Wait();
				client.Destroy();
			}
			protected override bool DestroyClientCondition(IPooledRedisClient pooledRedisClient)
			{
				if (pooledRedisClient.State == RedisClient.ClientState.Subscription)
					return false;
				return base.DestroyClientCondition(pooledRedisClient);
			}
		}
		private readonly IRedisClientsPool _clientsPool;
		public PubSubClientsPool()
		{
			_clientsPool = new PubSubPool();
		}
		
		public IRedisSubscription GetClient(RedisConnectionStringBuilder connectionString)
		{
			return (IRedisSubscription)_clientsPool.CreateClientAsync(connectionString).Result;
		}
	}
}
