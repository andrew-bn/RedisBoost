using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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

	}
}
