using System;
using System.Linq;
using System.Text;
using Moq;
using NUnit.Framework;
using RedisBoost.Core.AsyncSocket;
using RedisBoost.Core.Sender;
using RedisBoost.Misk;

namespace RedisBoost.Tests.Core
{
	[TestFixture]
	public class RedisSenderTests
	{
		private Mock<IAsyncSocket> _asyncSocket;
		private Mock<IBuffersPool> _buffersPool;
		private byte[][] _dataToSend;
		private string _expectedSentData;
		private byte[] _bufferFromPool;
		[SetUp]
		public void Setup()
		{
			_bufferFromPool = new byte[1024];
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

			_buffersPool = new Mock<IBuffersPool>();
			_buffersPool.Setup(b => b.TryGet(out _bufferFromPool, It.IsAny<Action<byte[]>>()))
					.Returns(true);
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
			byte[] actualBuffer = null;
			_asyncSocket.Setup(s => s.Send(It.IsAny<AsyncSocketEventArgs>())).Callback(
				(AsyncSocketEventArgs a) => { actualBuffer = a.BufferList[0].Array; });

			args.DataToSend = _dataToSend;

			var sender = CreateSender();

			sender.Send(args);

			Assert.IsTrue(ArrayIsEqualTo(actualBuffer, _expectedSentData));
			
		}
		[Test]
		public void Flush()
		{
			byte[] actualBuffer = null;
			_asyncSocket.Setup(s => s.Send(It.IsAny<AsyncSocketEventArgs>())).Callback(
				(AsyncSocketEventArgs a) => { actualBuffer = a.BufferList[0].Array; });

			var args = new SenderAsyncEventArgs();
			args.DataToSend = _dataToSend;

			var sender = CreateSender(autoFlush: false);

			sender.Send(args);
			sender.Flush(args);

			Assert.IsTrue(ArrayIsEqualTo(actualBuffer, _expectedSentData));
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
			_bufferFromPool = new byte[5];
			_buffersPool.Setup(b => b.TryGet(out _bufferFromPool, It.IsAny<Action<byte[]>>()))
					.Returns(true);
			var expectedLine = "*3\r\n";
			string actual = null;
			_asyncSocket.Setup(s => s.Send(It.IsAny<AsyncSocketEventArgs>()))
						.Callback((AsyncSocketEventArgs a) => { actual = actual ?? ConvertToString(a.BufferList[0].Array, a.BufferList[0].Count); });
			var args = new SenderAsyncEventArgs();
			args.DataToSend = _dataToSend;
			var sender = CreateSender();
			sender.Send(args);

			Assert.AreEqual(expectedLine, actual);

		}

		[Test]
		public void Send_AutoFlushIsOff_IfBufferIsFullThenWritingArrayItem()
		{
			_bufferFromPool = new byte[10];
			_buffersPool.Setup(b => b.TryGet(out _bufferFromPool, It.IsAny<Action<byte[]>>()))
					.Returns(true);
			var expectedLine = "*3\r\n$9\r\nFi";
			string actual = null;
			_asyncSocket.Setup(s => s.Send(It.IsAny<AsyncSocketEventArgs>()))
						.Callback((AsyncSocketEventArgs a) => { actual = actual ?? ConvertToString(a.BufferList[0].Array, a.BufferList[0].Count); });
			var args = new SenderAsyncEventArgs();
			args.DataToSend = _dataToSend;
			var sender = CreateSender();
			sender.Send(args);
			Assert.AreEqual(expectedLine, actual);
		}

		[Test]
		public void Send_SocketAsyncExecution_CallbackIsNotCalled()
		{

			var called = false;
			_asyncSocket.Setup(s => s.Send(It.IsAny<AsyncSocketEventArgs>()))
				.Callback((AsyncSocketEventArgs a) =>a.Completed(a))
				.Returns(true);

			var args = new SenderAsyncEventArgs();
			args.DataToSend = _dataToSend;
			args.Completed = a => { called = true; };
			
			var sender = CreateSender();

			sender.Send(args);

			Assert.IsFalse(called);
		}

		[Test]
		public void Send_AutoFlushIsOn_LimitedBuffer_AsyncExecution_DataSentCorrectly()
		{
			_bufferFromPool = new byte[5];
			_buffersPool.Setup(b => b.TryGet(out _bufferFromPool, It.IsAny<Action<byte[]>>()))
					.Returns(true);
			var actual = "";
			_asyncSocket.Setup(s => s.Send(It.IsAny<AsyncSocketEventArgs>()))
				.Callback((AsyncSocketEventArgs a) =>
				{
					actual += ConvertToString(a.BufferList[0].Array, a.BufferList[0].Count);
					a.Completed(a);
				})
				.Returns(true);

			var args = new SenderAsyncEventArgs();
			args.DataToSend = _dataToSend;
			args.Completed = s => { };
			var sender = CreateSender();
			sender.Send(args);

			Assert.AreEqual(_expectedSentData, actual);
		}
		[Test]
		public void Send_AutoFlushIsOn_LimitedBuffer_SyncExecution_DataSentCorrectly()
		{
			_bufferFromPool = new byte[5];
			_buffersPool.Setup(b => b.TryGet(out _bufferFromPool, It.IsAny<Action<byte[]>>()))
					.Returns(true);
			var actual = "";
			_asyncSocket.Setup(s => s.Send(It.IsAny<AsyncSocketEventArgs>()))
				.Callback((AsyncSocketEventArgs a) =>
				{
					actual += ConvertToString(a.BufferList[0].Array, a.BufferList[0].Count);
				})
				.Returns(false);

			var args = new SenderAsyncEventArgs();
			args.DataToSend = _dataToSend;
			args.Completed = s => { };
			var sender = CreateSender();
			sender.Send(args);

			Assert.AreEqual(_expectedSentData, actual);
		}

		private RedisSender CreateSender(bool autoFlush = true)
		{
			return new RedisSender(_buffersPool.Object, _asyncSocket.Object, autoFlush);
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
