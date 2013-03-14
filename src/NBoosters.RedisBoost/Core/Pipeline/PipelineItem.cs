using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBoosters.RedisBoost.Core.Pipeline
{
	internal struct PipelineItem
	{
		public PipelineItem(byte[][] request, Action<Exception, RedisResponse> callBack)
		{
			CallBack = callBack;
			Request = request;
		}

		public byte[][] Request;
		public Action<Exception, RedisResponse> CallBack;
	}
}
