using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace NBoosters.RedisBoost.Core.AsyncSocket
{
	internal interface ISocket
	{
		bool SendAsync(SocketAsyncEventArgs args);
		bool ReceiveAsync(SocketAsyncEventArgs args);
		bool ConnectAsync(SocketAsyncEventArgs args);
		bool DisconnectAsync(SocketAsyncEventArgs args);
	}
}
