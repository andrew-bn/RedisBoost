using System;
using System.Threading.Tasks;

namespace NBoosters.RedisBoost.Core.Pipeline
{
	internal interface IRedisPipeline
	{
		void ExecuteCommandAsync(byte[][] args, Action<Exception, RedisResponse> callBack);
		void ClosePipeline();
	}
}
