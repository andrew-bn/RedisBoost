using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBoosters.RedisBoost.Core.Pipeline
{
	internal class PipelineItem
	{
		public PipelineItem(byte[][] request, TaskCompletionSource<RedisResponse> tcs)
		{
			TaskCompletionSource = tcs;
			Request = request;
		}

		public byte[][] Request { get; private set; }
		public TaskCompletionSource<RedisResponse> TaskCompletionSource { get; private set; }
	}
}
