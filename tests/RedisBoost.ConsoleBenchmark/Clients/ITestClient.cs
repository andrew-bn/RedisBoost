using System;
using System.Threading.Tasks;

namespace RedisBoost.ConsoleBenchmark.Clients
{
	public interface ITestClient:IDisposable
	{
		string ClientName { get; }
		void Connect(RedisConnectionStringBuilder connectionString);
		void SetAsync(string key, string value);
		void Set(string key, string value);
		string GetString(string key);
		void FlushDb();
		void IncrAsync(string KeyName);
		int GetInt(string key);
		ITestClient CreateOne();
	}
}
