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
		private const int Iter = 100000;
		private static NBoosters.RedisBoost.RedisConnectionStringBuilder _cs;
		static void Main(string[] args)
		{
			_cs = new RedisConnectionStringBuilder(ConfigurationManager.ConnectionStrings["Redis"].ConnectionString);

			Console.WriteLine("BookSleeve = " + RunBookSleeveTest());
			Console.WriteLine("Csredis = " + RunCsredisTest());
			Console.WriteLine("NBoosters = " + RunNBoostersTest());

			Console.ReadKey();
		}
		private static int RunNBoostersTest()
		{
			var conn = RedisClient.ConnectAsync(_cs.EndPoint, _cs.DbIndex).Result;
			
			conn.FlushDbAsync().Wait();
			conn.IncrAsync("NBoosters").Wait();
			conn.FlushDbAsync().Wait();

			var sw = new Stopwatch();
			sw.Start();

			for (int i = 0; i < Iter; i++)
				conn.IncrAsync("NBoosters");

			var result = conn.GetAsync("NBoosters").Result.As<int>();
			if (result != Iter)
				Console.WriteLine("NBoosters result error");

			sw.Stop();
			return (int)sw.ElapsedMilliseconds;
		}

		private static int RunBookSleeveTest()
		{
			var conn = new BookSleeve.RedisConnection(((IPEndPoint)_cs.EndPoint).Address.ToString(), allowAdmin: true);
			conn.Open();
			conn.Server.FlushDb(_cs.DbIndex).Wait();
			conn.Strings.Increment(_cs.DbIndex, "BookSleeve").Wait();
			conn.Server.FlushDb(_cs.DbIndex);

			var sw = new Stopwatch();
			sw.Start();

			for (int i = 0; i < Iter; i++)
				conn.Strings.Increment(_cs.DbIndex, "BookSleeve");

			var result = conn.Strings.GetInt64(_cs.DbIndex, "BookSleeve").Result;
			if (result != Iter)
				Console.WriteLine("BookSleeve result error");

			sw.Stop();
			return (int)sw.ElapsedMilliseconds;
		}

		private static int RunCsredisTest()
		{
			var conn = new ctstone.Redis.RedisClientAsync(((IPEndPoint)_cs.EndPoint).Address.ToString(), ((IPEndPoint)_cs.EndPoint).Port,10000);
			
			conn.FlushDb().Wait();
			conn.Incr("Csredis").Wait();
			conn.FlushDb().Wait();

			var sw = new Stopwatch();
			sw.Start();

			for (int i = 0; i < Iter; i++)
				conn.Incr("Csredis");

			var result = int.Parse(conn.Get("Csredis").Result);
			if (result != Iter)
				Console.WriteLine("csredis result error");

			sw.Stop();
			return (int)sw.ElapsedMilliseconds;
		}
	}
}
