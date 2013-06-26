using System.Net;
using System.Threading.Tasks;
using BookSleeve;

namespace RedisBoost.ConsoleBenchmark.Clients
{
	public class BookSleeveTestClient: ITestClient
	{
		private RedisConnection _client;
		private int _dbIndex;
		public void Dispose()
		{
			if (_client!=null)
				_client.Dispose();
		}

		public void Connect(RedisConnectionStringBuilder connectionString)
		{
			_client = new RedisConnection(((IPEndPoint) connectionString.EndPoint).Address.ToString(), allowAdmin: true);
			_client.Open();
			_dbIndex = connectionString.DbIndex;
		}

		public void SetAsync(string key, string value)
		{
			_client.Strings.Set(_dbIndex, key, value);
		}

		public string GetString(string key)
		{
			return _client.Strings.GetString(_dbIndex, key).Result;
		}

		public void FlushDb()
		{
			_client.Server.FlushDb(_dbIndex).Wait();
		}

		public string ClientName
		{
			get { return "booksleeve"; }
		}

		public void IncrAsync(string key)
		{
			_client.Strings.Increment(_dbIndex, key);
		}

		public int GetInt(string key)
		{
			return (int)_client.Strings.GetInt64(_dbIndex, key).Result;
		}

		public ITestClient CreateOne()
		{
			return new BookSleeveTestClient();
		}

		public void Set(string key, string value)
		{
			_client.Strings.Set(_dbIndex, key, value).Wait();
		}
	}
}
