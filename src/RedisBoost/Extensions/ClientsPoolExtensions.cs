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

using System.Net;
using RedisBoost.Core.Serialization;
using RedisBoost.Extensions;

namespace RedisBoost
{
	internal static class ClientsPoolExtensions
	{
		public static IExtensionsFactory CreateExtensionsFactory(this IRedisClientsPool pool, string connectionString, BasicRedisSerializer serializer = null)
		{
			return new ExtensionsFactory(pool, new RedisConnectionStringBuilder(connectionString),serializer);
		}
		public static IExtensionsFactory CreateExtensionsFactory(this IRedisClientsPool pool, string host, int port, int dbIndex = 0, BasicRedisSerializer serializer = null)
		{
			return new ExtensionsFactory(pool, new RedisConnectionStringBuilder(host,port,dbIndex), serializer);
		}
		public static IExtensionsFactory CreateExtensionsFactory(this IRedisClientsPool pool, EndPoint endPoint, int dbIndex = 0, BasicRedisSerializer serializer = null)
		{
			return new ExtensionsFactory(pool, new RedisConnectionStringBuilder(endPoint, dbIndex), serializer);
		}
	}
}
