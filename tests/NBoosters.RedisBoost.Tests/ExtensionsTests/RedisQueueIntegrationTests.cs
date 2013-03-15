using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBoosters.RedisBoost.Extensions;
using NBoosters.RedisBoost.Extensions.Queue;
using NUnit.Framework;

namespace NBoosters.RedisBoost.Tests.ExtensionsTests
{
	[TestFixture]
	public class RedisQueueIntegrationTests
	{
		[Test]
		public void Enqueue_Dequeue()
		{
			var queue = CreateQueue<string>();
			Assert.AreEqual(1, queue.Enqueue("value1"));
			Assert.AreEqual(2, queue.Enqueue("value2"));

			Assert.AreEqual("value1", queue.Dequeue());
			Assert.AreEqual("value2", queue.Dequeue());
		}
		[Test]
		public void Count()
		{
			var queue = CreateQueue<string>();
			Assert.AreEqual(1, queue.Enqueue("value1"));
			Assert.AreEqual(2, queue.Enqueue("value2"));
			Assert.AreEqual(2,queue.Count);

			Assert.AreEqual("value1", queue.Dequeue());
			Assert.AreEqual(1, queue.Count);
		}
		[Test]
		public void Peek()
		{
			var queue = CreateQueue<string>();
			Assert.AreEqual(1, queue.Enqueue("value1"));
			Assert.AreEqual(2, queue.Enqueue("value2"));
			Assert.AreEqual("value1", queue.Peek());
			Assert.AreEqual(2, queue.Count);
		}
		[Test]
		public void Clear()
		{
			var queue = CreateQueue<string>();
			Assert.AreEqual(1, queue.Enqueue("value1"));
			Assert.AreEqual(2, queue.Enqueue("value2"));
			queue.Clear();
			Assert.AreEqual(0, queue.Count);
		}
		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void DequeueOfEmptyQueue()
		{
			var queue = CreateQueue<string>();
			queue.Dequeue();
		}
		[Test]
		public void TryDequeueOfEmptyQueue()
		{
			var queue = CreateQueue<string>();
			string result;
			Assert.IsFalse(queue.TryDequeue(out result));
		}
		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void PeekOfEmptyQueue()
		{
			var queue = CreateQueue<string>();
			queue.Peek();
		}
		[Test]
		public void TryPeekOfEmptyQueue()
		{
			var queue = CreateQueue<string>();
			string result;
			Assert.IsFalse(queue.TryPeek(out result));
		}
		private IQueue<T> CreateQueue<T>()
		{
			var cli = RedisClient.ConnectAsync(ConnectionString).Result;
			cli.FlushDbAsync().Wait();
			cli.QuitAsync().Wait();
			cli.Dispose();
			return RedisClient.CreateClientsPool()
							  .CreateExtensionsFactory(ConnectionString)
							  .CreateQueue<T>("queue");
		}
		private string ConnectionString
		{
			get { return ConfigurationManager.ConnectionStrings["Redis"].ConnectionString; }
		}
	}
}
