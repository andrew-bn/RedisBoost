using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RedisBoost.Extensions.ManualResetEvent;

namespace RedisBoost.Tests.ExtensionsTests
{
	[TestFixture]
	public class ManualResetEventTests
	{
		[Test]
		public void SignaledState_NoBlocking()
		{
			var ev = CreateEvent();
			var result = ev.WaitOneAsync().Result;
			Assert.AreEqual(true,result);
		}
		[Test]
		public void SignaledState_BlocksExecution()
		{
			var ev = CreateEvent();
			ev.Reset();
			int result = 0;
			Task.Run(() =>
				{
					ev.WaitOneAsync().Wait();
					result = 1;
				});
			Thread.Sleep(1000);
			Assert.AreEqual(0,result);
			ev.Set();
			Thread.Sleep(100);
			Assert.AreEqual(1,result);
		}
		private IManualResetEvent CreateEvent()
		{
			var cli = RedisClient.ConnectAsync(ConnectionString).Result;
			cli.FlushDbAsync().Wait();
			cli.QuitAsync().Wait();
			cli.Dispose();
			return RedisClient.CreateClientsPool()
							  .CreateExtensionsFactory(ConnectionString)
							  .CreateManualResetEvent("event");
		}
		private string ConnectionString
		{
			get { return ConfigurationManager.ConnectionStrings["Redis"].ConnectionString; }
		}
	}
}
