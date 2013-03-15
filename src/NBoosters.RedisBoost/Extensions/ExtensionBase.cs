using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NBoosters.RedisBoost.Core.Misk;
using NBoosters.RedisBoost.Core.Serialization;

namespace NBoosters.RedisBoost.Extensions
{
	internal abstract class ExtensionBase
	{
		private readonly IRedisClientsPool _pool;
		private readonly RedisConnectionStringBuilder _connectionStringBuilder;
		private readonly BasicRedisSerializer _serializer;

		protected ExtensionBase(IRedisClientsPool pool, RedisConnectionStringBuilder connectionStringBuilder, BasicRedisSerializer serializer)
		{
			_pool = pool;
			_connectionStringBuilder = connectionStringBuilder;
			_serializer = serializer;
		}

		protected IRedisClient GetClient()
		{
			return _pool.CreateClientAsync(_connectionStringBuilder, _serializer).Result;
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
