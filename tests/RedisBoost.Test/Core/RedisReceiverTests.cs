using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using RedisBoost.Core.Serialization;
using RedisBoost.Core.AsyncSocket;
using RedisBoost.Core.Receiver;
using RedisBoost.Misk;
using Xunit;

namespace RedisBoost.Tests.Core
{
	public class RedisReceiverTests
	{
		private Mock<IAsyncSocket> _asyncSocket;
		private Mock<IBuffersPool> _buffersPool;
		private ReceiverAsyncEventArgs _args;
		private byte[] _dataBuffer;
		private byte[] _bufferFromPool;

		public RedisReceiverTests()
		{
			_bufferFromPool = new byte[1024];
			_args = new ReceiverAsyncEventArgs();
			_asyncSocket = new Mock<IAsyncSocket>();

			_asyncSocket.Setup(s => s.Receive(It.IsAny<AsyncSocketEventArgs>()))
						.Callback((AsyncSocketEventArgs a) =>
						{
							a.DataLength = _dataBuffer.Length;
							Array.Copy(_dataBuffer, a.BufferToReceive, _dataBuffer.Length);
						});

			_buffersPool = new Mock<IBuffersPool>();
			_buffersPool.Setup(b => b.TryGet(out _bufferFromPool, It.IsAny<Action<byte[]>>()))
						.Returns(true);
		}
		[Fact]
		public void RedisInteger_ParsedCorrectly()
		{
			_dataBuffer = ConvertToBytes(":23\r\n");

			var r = CreateReceiver();
			r.Receive(_args);

			Assert.Equal(ResponseType.Integer, _args.Response.ResponseType);
			Assert.Equal(23, _args.Response.AsInteger());
		}
		[Fact]
		public void RedisStatus_ParsedCorrectly()
		{
			_dataBuffer = ConvertToBytes("+SomeLine\r\n");

			var r = CreateReceiver();
			r.Receive(_args);

			Assert.Equal(ResponseType.Status, _args.Response.ResponseType);
			Assert.Equal("SomeLine", _args.Response.ToString());
		}
		[Fact]
		public void RedisError_ParsedCorrectly()
		{
			_dataBuffer = ConvertToBytes("-SomeError\r\n");

			var r = CreateReceiver();
			r.Receive(_args);

			Assert.Equal(ResponseType.Error, _args.Response.ResponseType);
			Assert.Equal("SomeError", _args.Response.ToString());
		}
		[Fact]
		public void RedisBulk_ParsedCorrectly()
		{
			_dataBuffer = ConvertToBytes("$4\r\nBulk\r\n");

			var r = CreateReceiver();
			r.Receive(_args);

			Assert.Equal(ResponseType.Bulk, _args.Response.ResponseType);
			Assert.True(ConvertToBytes("Bulk").SequenceEqual((byte[])_args.Response));
		}

		[Fact]
		public void RedisMultiBulk_ParsedCorrectly()
		{
			_dataBuffer = ConvertToBytes("*3\r\n$3\r\nSET\r\n+Status\r\n$7\r\nmyvalue\r\n");

			var r = CreateReceiver();
			r.Receive(_args);

			Assert.Equal(ResponseType.MultiBulk, _args.Response.ResponseType);
			var mb = _args.Response.AsMultiBulk();
			Assert.Equal(3, mb.Length);
			Assert.Equal(ResponseType.Bulk, mb[0].ResponseType);
			Assert.Equal(ResponseType.Status, mb[1].ResponseType);
			Assert.Equal(ResponseType.Bulk, mb[2].ResponseType);
		}

		[Fact]
		public void RedisMultiBulk_AsyncMode_ParsedCorrectly()
		{
			int _offcet = 0;
			_asyncSocket.Setup(s => s.Receive(It.IsAny<AsyncSocketEventArgs>()))
					.Callback((AsyncSocketEventArgs a) =>
					{
						a.DataLength = 1;

						Array.Copy(_dataBuffer, _offcet, a.BufferToReceive, 0, 1);
						_offcet++;
						a.Completed(a);
					}).Returns(true);

			_dataBuffer = ConvertToBytes("*3\r\n$3\r\nSET\r\n$5\r\nmykey\r\n$7\r\nmyvalue\r\n");
			var completed = false;
			_args.Completed = a => completed = true;
			var r = CreateReceiver();
			r.Receive(_args);

			Assert.True(completed);
			Assert.Equal(ResponseType.MultiBulk, _args.Response.ResponseType);
			Assert.Equal(3, _args.Response.AsMultiBulk().Length);
			Assert.Equal(ResponseType.Bulk, _args.Response.AsMultiBulk()[0].ResponseType);
			Assert.Equal(5, _args.Response.AsMultiBulk()[1].AsBulk().Length);
			Assert.Equal(7, _args.Response.AsMultiBulk()[2].AsBulk().Length);
		}

		private byte[] ConvertToBytes(string data)
		{
			return Encoding.UTF8.GetBytes(data);
		}

		private RedisReceiver CreateReceiver()
		{
			return new RedisReceiver(_buffersPool.Object, _asyncSocket.Object);
		}
	}
}
