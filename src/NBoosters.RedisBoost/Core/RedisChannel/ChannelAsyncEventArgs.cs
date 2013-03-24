using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace NBoosters.RedisBoost.Core.RedisChannel
{
	internal class ChannelAsyncEventArgs
	{
		public Exception Exception { get; set; }
		public RedisResponse RedisResponse { get; set; }
		public int ReceiveMultiBulkPartsLeft { get; set; }
		public RedisResponse[] MultiBulkParts { get; set; }
		public Action<ChannelAsyncEventArgs> Completed { get; set; }
	}
}
