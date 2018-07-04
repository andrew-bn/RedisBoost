using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RedisBoost.Core.AsyncSocket;

namespace RedisBoost.Tests.Core
{
	[TestClass]
	public class AsyncSocketTests
	{
		private Mock<ISocket> _socket;
		public AsyncSocketTests()
		{
			_socket = new Mock<ISocket>();
		}

		[TestMethod]
		public void Connect_CallsSocketConnect()
		{
			var ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"),1234);
			var sock = CreateSocket();
			var args = new AsyncSocketEventArgs();
			args.RemoteEndPoint = ep;
			sock.Connect(args);

			_socket.Verify(s=>s.ConnectAsync(
								It.Is((SocketAsyncEventArgs a)=> a.RemoteEndPoint == ep)));
		}
		[TestMethod]
		public void Connect_ReturnsTrueIsOperationIsAsync()
		{
			_socket.Setup(s => s.ConnectAsync(It.IsAny<SocketAsyncEventArgs>())).Returns(true);
			var result = CreateSocket().Connect(new AsyncSocketEventArgs());
			Assert.IsTrue(result);
		}
		[TestMethod]
		public void Connect_ReturnsFalseIsOperationIsAsync()
		{
			_socket.Setup(s => s.ConnectAsync(It.IsAny<SocketAsyncEventArgs>())).Returns(false);
			var result = CreateSocket().Connect(new AsyncSocketEventArgs());
			Assert.IsFalse(result);
		}
		[TestMethod]
		public void Disconnect_CallsSocketConnect()
		{
			var args = new AsyncSocketEventArgs();
			var sock = CreateSocket();
			sock.Disconnect(args);
			_socket.Verify(s => s.DisconnectAsync(It.IsAny<SocketAsyncEventArgs>()));
		}

		[TestMethod]
		public void Disconnect_ReturnsTrueIsOperationIsAsync()
		{
			_socket.Setup(s => s.DisconnectAsync(It.IsAny<SocketAsyncEventArgs>())).Returns(true);
			var result = CreateSocket().Disconnect(new AsyncSocketEventArgs());
			Assert.IsTrue(result);
		}
		[TestMethod]
		public void Disconnect_ReturnsFalseIsOperationIsAsync()
		{
			_socket.Setup(s => s.DisconnectAsync(It.IsAny<SocketAsyncEventArgs>())).Returns(false);
			var result = CreateSocket().Disconnect(new AsyncSocketEventArgs());
			Assert.IsFalse(result);
		}
		[TestMethod]
		public void NotIoSyncOperation_CompletedNotCalled()
		{
			_socket.Setup(s => s.DisconnectAsync(It.IsAny<SocketAsyncEventArgs>())).Returns(false);

			var args = new AsyncSocketEventArgs();
			var called = false;
			args.Completed = (e) => { called = true; };

			var sock = CreateSocket();
			sock.Disconnect(args);
			
			Assert.IsFalse(called);
		}

		[TestMethod]
		public void NotIoAsyncOperation_CompletedCalled()
		{
			_socket.Setup(s => s.DisconnectAsync(It.IsAny<SocketAsyncEventArgs>()))
				.Callback((SocketAsyncEventArgs a) => { CallOnCompleted(a); })
				.Returns(true);

			var args = new AsyncSocketEventArgs();
			var called = false;
			args.Completed = (e) => { called = true; };

			var sock = CreateSocket();
			sock.Disconnect(args);

			Assert.IsTrue(called);
		}

		[TestMethod]
		public void Receive_CallsSocketReceive()
		{
			const int bufferSize = 111;
			var sock = CreateSocket();
			var args = new AsyncSocketEventArgs();
			args.BufferToReceive = new byte[bufferSize];
			sock.Receive(args);

			_socket.Verify(s => s.ReceiveAsync(
								It.Is((SocketAsyncEventArgs a) => a.Count == bufferSize &&
								a.Offset == 0)));
		}
		[TestMethod]
		public void Receive_ReturnsTrueIsOperationIsAsync()
		{
			_socket.Setup(s => s.ReceiveAsync(It.IsAny<SocketAsyncEventArgs>())).Returns(true);

			var result = CreateSocket().Receive(new AsyncSocketEventArgs(){BufferToReceive = new byte[0]});
			Assert.IsTrue(result);
		}
		[TestMethod]
		public void Receive_ReturnsFalseIsOperationIsAsync()
		{
			_socket.Setup(s => s.ReceiveAsync(It.IsAny<SocketAsyncEventArgs>())).Returns(false);
			var result = CreateSocket().Receive(new AsyncSocketEventArgs() { BufferToReceive = new byte[0] });
			Assert.IsFalse(result);
		}


		private void CallOnCompleted(SocketAsyncEventArgs a)
		{
			var method = typeof (SocketAsyncEventArgs).GetMethod("OnCompleted", BindingFlags.Instance|BindingFlags.NonPublic);
			method.Invoke(a, new object[]{a});
		}

		private AsyncSocketWrapper CreateSocket()
		{
			var result = new AsyncSocketWrapper();
			result.EngageWith(_socket.Object);
			return result;
		}

	}
}
