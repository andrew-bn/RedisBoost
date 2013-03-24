using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace NBoosters.RedisBoost.Core.RedisStream
{
	internal class StreamAsyncEventArgs
	{
		public StreamAsyncEventArgs()
		{
			LineBuffer = new StringBuilder();
		}

		public bool HasException { get { return Exception != null; } }
		public int Offset { get; set; }
		public int Length { get; set; }
		public byte[] Block { get; set; }
		public Exception Exception { get; set; }
		public byte FirstChar { get; set; }
		public string Line { get; set; }
		public StringBuilder LineBuffer { get; private set; }
		public Action<StreamAsyncEventArgs> Completed { get; set; }
	}
}
