using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NBoosters.RedisBoost.Core.AsyncSocket
{
	internal interface IAsyncSocket : ISocketDependent
	{
		bool Send(AsyncSocketEventArgs eventArgs);
		bool Receive(AsyncSocketEventArgs eventArgs);
		bool Connect(AsyncSocketEventArgs eventArgs);
		bool Disconnect(AsyncSocketEventArgs eventArgs);
	}
}
