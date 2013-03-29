using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NBoosters.RedisBoost.Core.Receiver
{
	internal class ReceiverAsyncEventArgs : EventArgsBase<ReceiverAsyncEventArgs>
	{
		public RedisResponse Response { get; set; }
	}
}
