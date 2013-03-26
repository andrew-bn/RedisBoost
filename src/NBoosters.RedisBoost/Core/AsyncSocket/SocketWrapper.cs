using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace NBoosters.RedisBoost.Core.AsyncSocket
{
	internal class SocketWrapper: ISocket
	{
		private readonly Socket _socket;

		public SocketWrapper(Socket socket)
		{
			_socket = socket;
		}

		public bool SendAsync(SocketAsyncEventArgs args)
		{
			return _socket.SendAsync(args);
		}

		public bool ReceiveAsync(SocketAsyncEventArgs args)
		{
			return _socket.ReceiveAsync(args);
		}

		public bool ConnectAsync(SocketAsyncEventArgs args)
		{
			return _socket.ConnectAsync(args);
		}

		public bool DisconnectAsync(SocketAsyncEventArgs args)
		{
			return _socket.DisconnectAsync(args);
		}
	}
}
