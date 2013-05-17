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
		private enum TestCase
		{
			SmallPack,
			MediumPack,
			LargePack,
			MixedPack,
		}
		private static int Iterations = 5;
		private const string KeyName = "K";
		private const string KeyName2 = "K2";
		private static int LoopSize = 2000000;
		private static RedisConnectionStringBuilder _cs;
		private static ITestClient[] _clients;

		static void Main(string[] args)
		{
			var testCase = InteractiveInitialization();
			_cs = new RedisConnectionStringBuilder(ConfigurationManager.ConnectionStrings["Redis"].ConnectionString);

			_clients = new ITestClient[]
				{
					new BookSleeveTestClient(),
					new RedisBoostTestClient(),
					new CsredisTestClient(),
				};

			switch (testCase)
			{
				case TestCase.SmallPack:
					Console.WriteLine("======== SMALL PAYLOAD ({0} chars) =========",Payloads.SmallPayload.Length);
					RunTestCase(Payloads.SmallPayload, LoopSize, testCase);
					break;
				case TestCase.MediumPack:
					Console.WriteLine("======= MEDIUM PAYLOAD ({0} chars) =========", Payloads.MediumPayload.Length);
					RunTestCase(Payloads.MediumPayload, LoopSize, testCase);
					break;
				case TestCase.LargePack:
					Console.WriteLine("======== LARGE PAYLOAD ({0} chars) =========", Payloads.LargePayload.Length);
					RunTestCase(Payloads.LargePayload, LoopSize, testCase);
					break;
				case TestCase.MixedPack:
					Console.WriteLine("======== MIXED PAYLOAD (INCR cmd and medium pack of size {1}) =========", 
						Payloads.SmallPayload.Length, Payloads.MediumPayload.Length);
					RunTestCase(Payloads.LargePayload, LoopSize, testCase);
					break;
			}
			Console.ReadKey();
		}

		private static TestCase InteractiveInitialization()
		{
			int temp;
			Console.WriteLine("Enter times the test will be executed /default is " + Iterations);
			if (int.TryParse(Console.ReadLine(), out temp)) Iterations = temp;

			Console.WriteLine("Enter iterations count in packet test /default is " + LoopSize);
			if (int.TryParse(Console.ReadLine(), out temp)) LoopSize = temp;

			while (true)
			{
				Console.Write(@"Choose one of these test cases:
0. Small packet
1. Medium packets
2. Large packets
3. Mixed packets
");
				int testCaseIndex;
				if (int.TryParse(Console.ReadLine(), out testCaseIndex) &&
				    testCaseIndex >= 0 && testCaseIndex <= Enum.GetNames(typeof (TestCase)).Length)
					return (TestCase) testCaseIndex;

				Console.WriteLine("Unknown Test Case index. Try ones more");
			}
		}

		private static void RunTestCase(string payload, int loopSize, TestCase testCase)
		{
			int[] avg = new int[_clients.Count()];

			for (int i = 0; i < Iterations; i++)
			{
				Console.WriteLine(i);
				for (int j = 0;j<_clients.Length;j++)
				{
					Console.Write(_clients[j].ClientName+" ~");
					var tmp = testCase != TestCase.MixedPack
								? RunBasicTest(_clients[j], payload, loopSize)
								: RunMixedPackTest(_clients[j], loopSize);
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
				Console.Write("[condition failed Result == Payload] ");

			sw.Stop();
			testClient.Dispose();
			return (int)sw.ElapsedMilliseconds;
			
		}

		private static int RunMixedPackTest(ITestClient testClient, int loopSize)
		{
			var payload = Payloads.MediumPayload;
			testClient.Connect(_cs);

			testClient.FlushDb();
			var sw = new Stopwatch();
			sw.Start();

			for (var i = 0; i < loopSize; i++)
			{
				testClient.SetAsync(KeyName, payload);
				testClient.IncrAsync(KeyName2);
			}

			var result = testClient.GetString(KeyName);
			var incrResult = testClient.GetInt(KeyName2);

			if (result != payload)
				Console.Write("[condition failed Result == Payload] ");
			if (incrResult != loopSize)
				Console.Write("[condition failed IncrResult == LoopSize] ");

			sw.Stop();
			testClient.Dispose();
			return (int)sw.ElapsedMilliseconds;
		}
	}
}
