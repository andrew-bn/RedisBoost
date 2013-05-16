using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NBoosters.RedisBoost.ConsoleBenchmark.Clients;

namespace NBoosters.RedisBoost.ConsoleBenchmark
{
	class Program
	{
		private static int Iterations = 5;
		private const string KeyName = "K";
		private static int LoopSize = 2000000;
		private static RedisConnectionStringBuilder _cs;
		private static ITestClient[] _clients;
		private const int MediumPayloadCoef = 50;
		private const int LargePayloadCoef = 100;
		static void Main(string[] args)
		{
			InteractiveInitialization();
			_cs = new RedisConnectionStringBuilder(ConfigurationManager.ConnectionStrings["Redis"].ConnectionString);

			_clients = new ITestClient[]
				{
					new BookSleeveTestClient(),
					new RedisBoostTestClient(),
					new CsredisTestClient(),
				};

			Console.WriteLine("======== SMALL PAYLOAD ({0} chars, {1} iterations) =========",Payloads.SmallPayload.Length, LoopSize);
			RunTestCase(Payloads.SmallPayload, LoopSize);
			Console.WriteLine("======= MEDIUM PAYLOAD ({0} chars, {1} iterations) =========", Payloads.MediumPayload.Length, LoopSize / MediumPayloadCoef);
			RunTestCase(Payloads.MediumPayload, LoopSize / MediumPayloadCoef);
			Console.WriteLine("======== LARGE PAYLOAD ({0} chars, {1} iterations) =========", Payloads.LargePayload.Length, LoopSize / LargePayloadCoef);
			RunTestCase(Payloads.LargePayload, LoopSize / LargePayloadCoef);
			Console.ReadKey();
		}

		private static void InteractiveInitialization()
		{
			int temp;
			Console.WriteLine("Enter times the test will be executed /default is " + Iterations);
			if (int.TryParse(Console.ReadLine(), out temp)) Iterations = temp;

			Console.WriteLine("Enter iterations count in each per test for smallPacketTest /default is " + LoopSize);
			if (int.TryParse(Console.ReadLine(), out temp)) LoopSize = temp;
		}

		private static void RunTestCase(string payload, int loopSize)
		{
			int[] avg = new int[_clients.Count()];

			for (int i = 0; i < Iterations; i++)
			{
				Console.WriteLine(i);
				for (int j = 0;j<_clients.Length;j++)
				{
					Console.Write(_clients[j].ClientName+" ~");
					var tmp = RunBasicTest(_clients[j], payload, loopSize);
					Console.WriteLine("{0}ms", tmp);
					avg[j] += tmp;
				}

				Console.WriteLine("---------");
			}
			Console.WriteLine("Avg:");
			for (int i = 0; i < avg.Length;i++ )
				Console.WriteLine("{0} ~{1}ms", _clients[i].ClientName,avg[i]/Iterations);
			Console.WriteLine();
		}
		private static int RunBasicTest(ITestClient testClient, string payload, int loopSize)
		{
			testClient.Connect(_cs);

			testClient.FlushDb();
			var sw = new Stopwatch();
			sw.Start();

			for (var i = 0; i < loopSize; i++)
				testClient.SetAsync(KeyName, payload);
		
			var result = testClient.GetString(KeyName);

			if (result != payload)
				Console.Write("[condition failed Result != Payload] ");

			sw.Stop();
			testClient.Dispose();
			return (int)sw.ElapsedMilliseconds;
			
		}
	}
}
