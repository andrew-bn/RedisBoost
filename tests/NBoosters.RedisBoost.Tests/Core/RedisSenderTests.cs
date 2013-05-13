using System.Linq;
using System.Text;
using Moq;
using NBoosters.RedisBoost.Core.AsyncSocket;
using NBoosters.RedisBoost.Core.Sender;
using NBoosters.RedisBoost.Misk;
using NUnit.Framework;

namespace NBoosters.RedisBoost.Tests.Core
{
	[TestFixture]
	public class RedisSenderTests
	{
		private Mock<IAsyncSocket> _asyncSocket;
		private Mock<IBuffersPool> _buffersPool;
		private byte[][] _dataToSend;
		private string _expectedSentData;
		[SetUp]
		public void Setup()
		{
			_buffersPool = new Mock<IBuffersPool>();
			_asyncSocket = new Mock<IAsyncSocket>();
			_dataToSend = new[]
				{
					ConvertToBytes("FirstLine"),
					ConvertToBytes("SecondLine"),
					ConvertToBytes("FirdLine"),
				};
			_expectedSentData = "*3\r\n";
			foreach (var item in _dataToSend)
			{
				_expectedSentData += "$" + item.Length + "\r\n";
				_expectedSentData += ConvertToString(item) + "\r\n";
			}
		}

		[Test]
		public void EngageWith_CallsAsyncSocketToSetupSystemSocket()
		{
			var sock = new Mock<ISocket>();
			CreateSender().EngageWith(sock.Object);
			_asyncSocket.Verify(s => s.EngageWith(sock.Object));
		}

		[Test]
		public void Send_DataSentCorrectly()
		{
			var args = new SenderAsyncEventArgs();
			args.DataToSend = _dataToSend;

			var sender = CreateSender();

			sender.Send(args);

			_asyncSocket.Verify(s => s.Send(
				It.Is((AsyncSocketEventArgs a) => ArrayIsEqualTo(a.DataToSend, _expectedSentData))));
		}
		[Test]
		public void Flush()
		{
			var args = new SenderAsyncEventArgs();
			args.DataToSend = _dataToSend;

			var sender = CreateSender(autoFlush: false);

			sender.Send(args);
			sender.Flush(args);

			_asyncSocket.Verify(s => s.Send(
				It.Is((AsyncSocketEventArgs a) => ArrayIsEqualTo(a.DataToSend, _expectedSentData))));
		}
		[Test]
		public void Send_AutoFlushIsOff_FillsBufferWithRedisArgumentsAmmountLine()
		{
			var args = new SenderAsyncEventArgs();
			args.DataToSend = _dataToSend;
			var sender = CreateSender(autoFlush: false);
			sender.Send(args);

			_asyncSocket.Verify(s => s.Send(It.IsAny<AsyncSocketEventArgs>()), Times.Never());
		}

		[Test]
		public void Send_AutoFlushIsOff_IfBufferIsFullThenWritingLineCount()
		{
			var expectedLine = "*3\r\n";
			string actual = null;
			_asyncSocket.Setup(s => s.Send(It.IsAny<AsyncSocketEventArgs>()))
						.Callback((AsyncSocketEventArgs a) => { actual = actual ?? ConvertToString(a.DataToSend, a.DataLength); });
			var args = new SenderAsyncEventArgs();
			args.DataToSend = _dataToSend;
			var sender = CreateSender(5);
			sender.Send(args);

			Assert.AreEqual(expectedLine, actual);

		}

		[Test]
		public void Send_AutoFlushIsOff_IfBufferIsFullThenWritingArrayItem()
		{
			var expectedLine = "*3\r\n$9\r\nFi";
			string actual = null;
			_asyncSocket.Setup(s => s.Send(It.IsAny<AsyncSocketEventArgs>()))
						.Callback((AsyncSocketEventArgs a) => { actual = actual ?? ConvertToString(a.DataToSend, a.DataLength); });
			var args = new SenderAsyncEventArgs();
			args.DataToSend = _dataToSend;
			var sender = CreateSender(10);
			sender.Send(args);
			Assert.AreEqual(expectedLine, actual);
		}

		[Test]
		public void Send_AsyncExecution_CallbackIsCalled()
		{
			SenderAsyncEventArgs actual = null;
			_asyncSocket.Setup(s => s.Send(It.IsAny<AsyncSocketEventArgs>()))
				.Callback((AsyncSocketEventArgs a) =>a.Completed(a))
				.Returns(true);

			var args = new SenderAsyncEventArgs();
			args.DataToSend = _dataToSend;
			args.Completed = a => { actual = a; };
			
			var sender = CreateSender();

			sender.Send(args);

			Assert.AreEqual(args, actual);
		}

		[Test]
		public void Send_AutoFlushIsOn_LimitedBuffer_AsyncExecution_DataSentCorrectly()
		{
			var actual = "";
			_asyncSocket.Setup(s => s.Send(It.IsAny<AsyncSocketEventArgs>()))
				.Callback((AsyncSocketEventArgs a) =>
				{
					actual += ConvertToString(a.DataToSend, a.DataLength);
					a.Completed(a);
				})
				.Returns(true);

			var args = new SenderAsyncEventArgs();
			args.DataToSend = _dataToSend;
			args.Completed = s => { };
			var sender = CreateSender(5);
			sender.Send(args);

			Assert.AreEqual(_expectedSentData, actual);
		}
		[Test]
		public void Send_AutoFlushIsOn_LimitedBuffer_SyncExecution_DataSentCorrectly()
		{
			var actual = "";
			_asyncSocket.Setup(s => s.Send(It.IsAny<AsyncSocketEventArgs>()))
				.Callback((AsyncSocketEventArgs a) =>
				{
					actual += ConvertToString(a.DataToSend, a.DataLength);
				})
				.Returns(false);

			var args = new SenderAsyncEventArgs();
			args.DataToSend = _dataToSend;
			args.Completed = s => { };
			var sender = CreateSender(5);
			sender.Send(args);

			Assert.AreEqual(_expectedSentData, actual);
		}

		private RedisSender CreateSender(int bufferSize = 1000, bool autoFlush = true)
		{
			return null;// new RedisSender(_buffersPool.Object, _asyncSocket.Object, bufferSize, autoFlush);
		}

		private byte[] ConvertToBytes(string data)
		{
			return Encoding.UTF8.GetBytes(data);
		}

		private string ConvertToString(byte[] data)
		{
			return ConvertToString(data, data.Length);
		}
		private string ConvertToString(byte[] data, int length)
		{
			return Encoding.UTF8.GetString(data, 0, length);
		}
		private bool ArrayIsEqualTo(byte[] source, string value)
		{
			var expected = ConvertToBytes(value);
			return source.Take(expected.Length).SequenceEqual(expected);
		}
	}
}
