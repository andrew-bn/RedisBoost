using System.Net;
using System.Threading.Tasks;
using ctstone.Redis;

namespace NBoosters.RedisBoost.ConsoleBenchmark.Clients
{
	public class CsredisTestClient: ITestClient
	{
		private RedisClientAsync _client;
		private ctstone.Redis.RedisClient _syncClient;
		public void Dispose()
		{
			if (_client!=null)
				_client.Dispose();
		}

		public void Connect(RedisConnectionStringBuilder connectionString)
		{
			_client = new RedisClientAsync(((IPEndPoint) connectionString.EndPoint).Address.ToString(),
			                                             ((IPEndPoint) connectionString.EndPoint).Port, 10000);
			_syncClient = new ctstone.Redis.RedisClient(((IPEndPoint) connectionString.EndPoint).Address.ToString(),
			                                             ((IPEndPoint) connectionString.EndPoint).Port, 10000);
		}

		public void SetAsync(string key, string value)
		{
			_client.Set(key, value);
		}

		public string GetString(string key)
		{
			return _client.Get(key).Result;
		}

		public void FlushDb()
		{
			_client.FlushDb().Wait();
		}

		public string ClientName
		{
			get { return "csredis"; }
		}
		public void IncrAsync(string key)
		{
			_client.Incr(key);
		}

		public int GetInt(string key)
		{
			return int.Parse(_client.Get(key).Result);
		}

		public ITestClient CreateOne()
		{
			return new CsredisTestClient();
		}

		public void Set(string key, string value)
		{
			_syncClient.Set(key, value);
		}

	}
}
