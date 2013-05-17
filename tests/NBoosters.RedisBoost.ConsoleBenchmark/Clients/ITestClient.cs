using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBoosters.RedisBoost.ConsoleBenchmark.Clients
{
	public interface ITestClient:IDisposable
	{
		string ClientName { get; }
		void Connect(RedisConnectionStringBuilder connectionString);
		Task SetAsync(string key, string value);
		string GetString(string key);
		void FlushDb();
		Task IncrAsync(string KeyName);
		int GetInt(string key);
		ITestClient CreateOne();
	}
}
