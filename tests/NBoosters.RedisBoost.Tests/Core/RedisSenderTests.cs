using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NBoosters.RedisBoost.Core;
using NBoosters.RedisBoost.Core.AsyncSocket;
using NBoosters.RedisBoost.Core.Sender;
using NUnit.Framework;

namespace NBoosters.RedisBoost.Tests.Core
{
	[TestFixture]
	public class RedisSenderTests
	{
		private Mock<IAsyncSocket> _asyncSocket;
		private Mock<IRedisDataAnalizer> _dataAnalizer;
		private byte[][] _dataToSend;
		[SetUp]
		public void Setup()
		{
			_asyncSocket= new Mock<IAsyncSocket>();
			_dataAnalizer = new Mock<IRedisDataAnalizer>();
			_dataToSend = new byte[][]
				{
					ConvertToBytes("FirstLine"),
					ConvertToBytes("SecondLine"),
					ConvertToBytes("FirdLine"),
				};
		}
		[Test]
		public void EngageWith_CallsAsyncSocketToSetupSystemSocket()
		{
			var sock = new Mock<ISocket>();
			CreateSender().EngageWith(sock.Object);

			_asyncSocket.Verify(s=>s.EngageWith(sock.Object));
		}

		[Test]
		public void Flush_CallsAsyncSocketSendMethod()
		{
			//CreateSender().Flush();
		}

		[Test]
		public void Send_FillsBufferWithRedisArgumentsAmmountLine()
		{
			var expectedLine = "*3\r\n";
			foreach (var item in _dataToSend)
			{
				expectedLine += "$" + item.Length + "\r\n";
				expectedLine +=ConvertToString(item) + "\r\n";
			}

			var args = new SenderAsyncEventArgs();
			args.DataToSend = _dataToSend;
			
			var sender = CreateSender();
			
			sender.Send(args);
			sender.Flush(args);

			_asyncSocket.Verify(s=>s.Send(
				It.Is((AsyncSocketEventArgs a)=> ArrayIsEqualTo(a.DataToSend, expectedLine))));
		}
		
		private RedisSender CreateSender()
		{
			return new RedisSender(_asyncSocket.Object);
		}

		private byte[] ConvertToBytes(string data)
		{
			return Encoding.UTF8.GetBytes(data);
		}
		private string ConvertToString(byte[] data)
		{
			return Encoding.UTF8.GetString(data);
		}
		private bool ArrayIsEqualTo(byte[] source, string value)
		{
			var expected = ConvertToBytes(value);
			return source.Take(expected.Length).SequenceEqual(expected);
		}
	}
}
