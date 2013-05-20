using System.Net;
using System.Threading.Tasks;
using BookSleeve;

namespace NBoosters.RedisBoost.ConsoleBenchmark.Clients
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

		public Task SetAsync(string key, string value)
		{
			return _client.Strings.Set(_dbIndex, key, value);
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

		public Task IncrAsync(string key)
		{
			return _client.Strings.Increment(_dbIndex, key);
		}

		public int GetInt(string key)
		{
			return (int)_client.Strings.GetInt64(_dbIndex, key).Result;
		}

		#region ITestClient Members


		public ITestClient CreateOne()
		{
			return new BookSleeveTestClient();
		}

		#endregion
	}
}
