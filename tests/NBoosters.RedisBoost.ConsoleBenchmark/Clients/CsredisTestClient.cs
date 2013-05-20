using System.Net;
using System.Threading.Tasks;
using ctstone.Redis;

namespace NBoosters.RedisBoost.ConsoleBenchmark.Clients
{
	public class CsredisTestClient: ITestClient
	{
		private RedisClientAsync _client;
		public void Dispose()
		{
			if (_client!=null)
				_client.Dispose();
		}

		public void Connect(RedisConnectionStringBuilder connectionString)
		{
			_client = new RedisClientAsync(((IPEndPoint) connectionString.EndPoint).Address.ToString(),
			                                             ((IPEndPoint) connectionString.EndPoint).Port, 10000);
		}

		public Task SetAsync(string key, string value)
		{
			return _client.Set(key, value);
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
		public Task IncrAsync(string key)
		{
			return _client.Incr(key);
		}

		public int GetInt(string key)
		{
			return int.Parse(_client.Get(key).Result);
		}

		#region ITestClient Members


		public ITestClient CreateOne()
		{
			return new CsredisTestClient();
		}

		#endregion
	}
}
