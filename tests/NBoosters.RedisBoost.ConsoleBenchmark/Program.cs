using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NBoosters.RedisBoost.ConsoleBenchmark
{
	class Program
	{
		private const int Iterations = 1;
		private const string KeyName = "K";
		private const string KeyName2 = "K2";
		private const int Iter = 50000;
		private static RedisConnectionStringBuilder _cs;
		static void Main(string[] args)
		{
			_cs = new RedisConnectionStringBuilder(ConfigurationManager.ConnectionStrings["Redis"].ConnectionString);

			RunTestCase(Payloads.SmallPayload);
			RunTestCase(Payloads.MediumPayload);
			RunTestCase(Payloads.LargePayload);
			Console.ReadKey();
		}

		private static void RunTestCase(string payload)
		{
			int bs = 0;
			int nb = 0;
			int cs = 0;
			for (int i = 0; i < Iterations; i++)
			{
				Console.WriteLine("Iteration " + i);
				var tmp = RunBookSleeveTest(payload);
				Console.WriteLine("booksleeve ~{0}ms", tmp);
				bs += tmp;

				tmp = RunNBoostersTest(payload);
				Console.WriteLine("redisboost ~{0}ms", tmp);
				nb += tmp;

				 tmp = RunCsredisTest(payload);
				Console.WriteLine("csredis    ~{0}ms", tmp);
				cs += tmp;

				Console.WriteLine("---------");
			}
			Console.WriteLine("Avg:");
			Console.WriteLine("booksleeve ~{0}ms", bs/Iterations);
			Console.WriteLine("redisboost ~{0}ms", nb/Iterations);
			Console.WriteLine("csredis    ~{0}ms", cs/Iterations);
			Console.WriteLine("=============");
			Console.WriteLine();
		}

		private static int RunNBoostersTest(string payload)
		{
			using (var conn = RedisClient.ConnectAsync(_cs.EndPoint, _cs.DbIndex).Result)
			{
				conn.FlushDbAsync().Wait();

				var sw = new Stopwatch();
				sw.Start();

				for (int i = 0; i < Iter; i++)
					conn.SetAsync(KeyName, payload);

				var result = conn.GetAsync(KeyName).Result.As<string>();
				if (result != payload)
					Console.WriteLine("NBoosters result error");

				sw.Stop();
				return (int) sw.ElapsedMilliseconds;
			}
		}

		private static int RunBookSleeveTest(string payload)
		{
			using (var conn = new BookSleeve.RedisConnection(((IPEndPoint) _cs.EndPoint).Address.ToString(), allowAdmin: true))
			{
				conn.Open();
				conn.Server.FlushDb(_cs.DbIndex);

				var sw = new Stopwatch();
				sw.Start();

				for (int i = 0; i < Iter; i++)
					conn.Strings.Set(_cs.DbIndex, KeyName, payload);

				var result = conn.Strings.GetString(_cs.DbIndex, KeyName).Result;
				if (result != payload)
					Console.WriteLine("BookSleeve result error");

				sw.Stop();
				return (int) sw.ElapsedMilliseconds;
			}
		}

		private static int RunCsredisTest(string payload)
		{
			using (
				var conn = new ctstone.Redis.RedisClientAsync(((IPEndPoint) _cs.EndPoint).Address.ToString(),
				                                              ((IPEndPoint) _cs.EndPoint).Port, 10000))
			{

				conn.FlushDb().Wait();

				var sw = new Stopwatch();
				sw.Start();

				for (int i = 0; i < Iter; i++)
				{
					conn.Set(KeyName, payload);
					conn.Incr(KeyName2);
				}

				var result = conn.Get(KeyName).Result;
				var count = int.Parse(conn.Get(KeyName2).Result);
				
				if (result != payload)
					Console.WriteLine("csredis result error");

				if (count != Iter)
					Console.WriteLine("csredis result error");

				sw.Stop();
				return (int) sw.ElapsedMilliseconds;
			}
		}
	}
}
