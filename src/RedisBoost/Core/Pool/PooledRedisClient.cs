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

using RedisBoost.Core.Serialization;

namespace RedisBoost.Core.Pool
{
	internal class PooledRedisClient : RedisClient, IPooledRedisClient
	{
		private readonly RedisClientsPool _pool;

		public PooledRedisClient(RedisClientsPool pool, RedisConnectionStringBuilder connectionString, BasicRedisSerializer serializer)
			: base(connectionString, serializer)
		{
			_pool = pool;
		}

		public void Destroy()
		{
			base.Dispose(true);
		}
		protected override void Dispose(bool disposing)
		{
			_pool.ReturnClient(this);
		}
	}
}