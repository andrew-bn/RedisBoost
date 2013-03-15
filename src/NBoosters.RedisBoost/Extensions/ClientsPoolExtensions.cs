using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using NBoosters.RedisBoost.Core.Serialization;
using NBoosters.RedisBoost.Extensions;

namespace NBoosters.RedisBoost
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
