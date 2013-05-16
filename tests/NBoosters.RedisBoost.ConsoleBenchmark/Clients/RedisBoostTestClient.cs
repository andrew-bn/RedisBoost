using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBoosters.RedisBoost.ConsoleBenchmark.Clients
{
	public class RedisBoostTestClient: ITestClient
	{
		private IRedisClient _client;
		public void Dispose()
		{
			if (_client!=null)
				_client.Dispose();
		}

		public void Connect(RedisConnectionStringBuilder connectionString)
		{
			_client = RedisClient.ConnectAsync(connectionString.EndPoint, connectionString.DbIndex).Result;
		}

		public Task SetAsync(string key, string value)
		{
			return _client.SetAsync(key, value);
		}

		public string GetString(string key)
		{
			return _client.GetAsync(key).Result.As<string>();
		}

		public void FlushDb()
		{
			_client.FlushDbAsync().Wait();
		}
		
		public string ClientName
		{
			get { return "redisboost"; }
		}
	}
}
