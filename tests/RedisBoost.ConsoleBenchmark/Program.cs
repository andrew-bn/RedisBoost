using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RedisBoost.ConsoleBenchmark.Clients;

namespace RedisBoost.ConsoleBenchmark
{
	class Program
	{
		private enum TestCase
		{
			SmallPack,
			MediumPack,
			LargePack,
			MixedPack,
			ParallelCli,
			SyncExecution,
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
					new RedisBoostTestClient(),
					new ServiceStackTestClient(),
					new BookSleeveTestClient(),
					new CsredisTestClient(),
				};

			switch (testCase)
			{
				case TestCase.SmallPack:
					Console.WriteLine("======== SMALL PAYLOAD ({0} chars) =========", Payloads.SmallPayload.Length);
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
					Console.WriteLine("======== MIXED PAYLOAD (INCR cmd and medium payload of size {0}) =========", Payloads.MediumPayload.Length);
					RunTestCase(Payloads.LargePayload, LoopSize, testCase);
					break;
				case TestCase.ParallelCli:
					Console.WriteLine("======== MIXED PAYLOAD (INCR cmd and medium payload of size {0}) =========", Payloads.MediumPayload.Length);
					RunTestCase(Payloads.LargePayload, LoopSize, testCase);
					break;
				case TestCase.SyncExecution:
					Console.WriteLine("======== SYNC EXECUTION (small payload of size {0}) =========", Payloads.SmallPayload.Length);
					RunTestCase(Payloads.SmallPayload, LoopSize, testCase);
					break;
			}
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
4. Parallel clients
5. Sync execution
");
				int testCaseIndex;
				if (int.TryParse(Console.ReadLine(), out testCaseIndex) &&
					testCaseIndex >= 0 && testCaseIndex <= Enum.GetNames(typeof(TestCase)).Length)
					return (TestCase)testCaseIndex;

				Console.WriteLine("Unknown Test Case index. Try ones more");
			}
		}

		private static void RunTestCase(string payload, int loopSize, TestCase testCase)
		{
			int[] avg = new int[_clients.Count()];

			for (int i = 0; i < Iterations; i++)
			{
				Console.WriteLine(i);
				for (int j = 0; j < _clients.Length; j++)
				{
					Console.Write(_clients[j].ClientName + " ~");
					int tmp = 0;

					if (testCase == TestCase.MixedPack)
						tmp = RunMixedPackTest(_clients[j], loopSize, KeyName, KeyName2);
					else if (testCase == TestCase.ParallelCli)
						tmp = RunManyClientsTest(_clients[j], loopSize);
					else if (testCase == TestCase.SyncExecution)
						tmp = RunSyncExecutionTest(payload, _clients[j], loopSize);
					else
						tmp = RunBasicTest(_clients[j], payload, loopSize);
 
					Console.WriteLine("{0}ms", tmp);
					avg[j] += tmp;
				}

				Console.WriteLine("---------");
			}
			Console.WriteLine("Avg:");
			for (int i = 0; i < avg.Length; i++)
				Console.WriteLine("{0} ~{1}ms", _clients[i].ClientName, avg[i] / Iterations);
			Console.WriteLine();
		}

		private static int RunSyncExecutionTest(string payload, ITestClient testClient, int loopSize)
		{
			using (testClient)
			{
				testClient.Connect(_cs);

				testClient.FlushDb();
				var sw = new Stopwatch();
				sw.Start();

				for (var i = 0; i < loopSize; i++)
					testClient.Set(KeyName, payload);

				var result = testClient.GetString(KeyName);

				if (result != payload)
					Console.Write("[condition failed Result == Payload] ");

				sw.Stop();
				return (int)sw.ElapsedMilliseconds;
			}
		}


		private static int RunBasicTest(ITestClient testClient, string payload, int loopSize)
		{
			using (testClient)
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
				return (int) sw.ElapsedMilliseconds;
			}
		}

		private static int RunMixedPackTest(ITestClient testClient, int loopSize, string key1, string key2, bool flushdb = true)
		{
			using (testClient)
			{
				var payload = Payloads.MediumPayload;
				testClient.Connect(_cs);
				if (flushdb)
					testClient.FlushDb();
				var sw = new Stopwatch();
				sw.Start();

				for (var i = 0; i < loopSize; i++)
				{
					testClient.SetAsync(key1, payload);
					testClient.IncrAsync(key2);
				}

				var result = testClient.GetString(key1);
				var incrResult = testClient.GetInt(key2);

				if (result != payload)
					Console.Write("[condition failed Result == Payload] ");
				if (incrResult != loopSize)
					Console.Write("[condition failed IncrResult == LoopSize] ");

				sw.Stop();
				return (int) sw.ElapsedMilliseconds;
			}
		}

		private static volatile int _clientsRunning = 0;
		private const int MaxClients = 10;
		private static int RunManyClientsTest(ITestClient client, int iterations)
		{
			var rand = new Random();
			_clientsRunning = 0;
			//warm up
			var sw = new Stopwatch();
			sw.Start();
			RunMixedPackTest(client.CreateOne(), iterations, KeyName, KeyName2);
			sw.Stop();

			var time = (int)sw.ElapsedMilliseconds;
			int spawnTimes = 0;
			int result = 0;
			var tasks = new List<Task>();
			for (var i = 0; i < Iterations; i++)
			{
				SpinWait.SpinUntil(() => _clientsRunning < MaxClients);

				tasks.Add(Task.Run(() =>
					{
						spawnTimes++;
						Console.WriteLine("Spawn new " + client.ClientName);
						Interlocked.Increment(ref _clientsRunning);
						var tmp = RunMixedPackTest(client.CreateOne(), iterations, KeyName + "_" + spawnTimes, KeyName2 + "_" + spawnTimes,false);
						result += tmp;
						Console.WriteLine("Client time ms = "+tmp);
						Interlocked.Decrement(ref _clientsRunning);
						Console.WriteLine("Destroy " + client.ClientName);
					}));
				Thread.Sleep(rand.Next(time));
			}
			Task.WaitAll(tasks.ToArray());
			return result/spawnTimes;
		}
	}
}
