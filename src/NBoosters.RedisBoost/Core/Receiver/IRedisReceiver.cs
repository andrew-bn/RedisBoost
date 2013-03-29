using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NBoosters.RedisBoost.Core.Receiver
{
	internal interface IRedisReceiver: ISocketDependent
	{
		bool Receive(ReceiverAsyncEventArgs args);
	}
}
