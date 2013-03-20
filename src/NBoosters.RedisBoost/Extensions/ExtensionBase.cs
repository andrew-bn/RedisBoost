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
using NBoosters.RedisBoost.Core.Misk;
using NBoosters.RedisBoost.Core.Serialization;

namespace NBoosters.RedisBoost.Extensions
{
	internal abstract class ExtensionBase
	{
		private readonly IRedisClientsPool _pool;
		protected RedisConnectionStringBuilder ConnectionStringBuilder { get; private set; }
		private readonly BasicRedisSerializer _serializer;

		protected ExtensionBase(IRedisClientsPool pool, RedisConnectionStringBuilder connectionStringBuilder, BasicRedisSerializer serializer)
		{
			_pool = pool;
			ConnectionStringBuilder = connectionStringBuilder;
			_serializer = serializer;
		}

		protected IRedisClient GetClient()
		{
			return _pool.CreateClientAsync(ConnectionStringBuilder, _serializer).Result;
		}
		protected static T ExecuteFunc<T>(Func<T> action)
		{
			try
			{
				return action();
			}
			catch (Exception ex)
			{
				throw ex.UnwrapAggregation();
			}
		}
		protected static void ExecuteAction(Action action)
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				throw ex.UnwrapAggregation();
			}
		}
	}
}
