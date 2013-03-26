using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NBoosters.RedisBoost.Core.Sender
{
	internal interface IRedisSender: ISocketDependent
	{
		bool Send(SenderAsyncEventArgs args);
		bool Flush(SenderAsyncEventArgs args);
	}
}
