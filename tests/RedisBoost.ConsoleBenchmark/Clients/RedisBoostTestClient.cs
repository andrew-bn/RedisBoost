using System.Threading.Tasks;

namespace RedisBoost.ConsoleBenchmark.Clients
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

		public void SetAsync(string key, string value)
		{
			_client.SetAsync(key, value);
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
		public void IncrAsync(string key)
		{
			_client.IncrAsync(key);
		}

		public int GetInt(string key)
		{
			return _client.GetAsync(key).Result.As<int>();
		}

		public ITestClient CreateOne()
		{
			return new RedisBoostTestClient();
		}

		public void Set(string key, string value)
		{
			_client.SetAsync(key, value).Wait();
		}

	}
}
