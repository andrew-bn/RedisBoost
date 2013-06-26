using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Redis;
using ServiceStack.Redis.Pipeline;

namespace RedisBoost.ConsoleBenchmark.Clients
{
	public class ServiceStackTestClient: ITestClient
	{
		private ServiceStack.Redis.RedisClient _client;
		private int _pipelineMode = 0;
		private IRedisPipeline _pipeline;
		public void Dispose()
		{
			if (_pipeline != null)
				_pipeline.Dispose();
			if (_client != null)
				_client.Dispose();
		}

		public string ClientName { get { return "servicestack"; } }
		public void Connect(RedisConnectionStringBuilder connectionString)
		{
			
			_client = new ServiceStack.Redis.RedisClient(((IPEndPoint)connectionString.EndPoint).Address.ToString(),
														 ((IPEndPoint)connectionString.EndPoint).Port);
		}

		public void SetAsync(string key, string value)
		{
			EnterPipeline();
			_pipeline.QueueCommand(c=>c.Set(key,value));
		}

		private void EnterPipeline()
		{
			if (Interlocked.CompareExchange(ref _pipelineMode, 1, 0) == 0)
				_pipeline = _client.CreatePipeline();
		}

		public void Set(string key, string value)
		{
			LeavePipelining();
			_client.Set(key, value);
		}

		private void LeavePipelining()
		{
			if (Interlocked.CompareExchange(ref _pipelineMode,0,1)==1)
			{
				_pipeline.Flush();
				_pipeline.Dispose();
			}
		}

		public string GetString(string key)
		{
			LeavePipelining();
			return _client.Get<string>(key);
		}

		public void FlushDb()
		{
			LeavePipelining();
			_client.FlushDb();
		}

		public void IncrAsync(string KeyName)
		{
			EnterPipeline();
			_pipeline.QueueCommand(c=>c.Increment(KeyName,1));
		}

		public int GetInt(string key)
		{
			LeavePipelining();
			return _client.Get<int>(key);
		}

		public ITestClient CreateOne()
		{
			return new ServiceStackTestClient();
		}
	}
}
