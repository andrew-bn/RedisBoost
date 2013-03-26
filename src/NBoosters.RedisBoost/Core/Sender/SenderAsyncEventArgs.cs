using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NBoosters.RedisBoost.Core.Sender
{
	internal class SenderAsyncEventArgs : EventArgsBase<SenderAsyncEventArgs>
	{
		public byte[][] DataToSend { get; set; }
	}
}
