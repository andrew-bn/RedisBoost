using System.Threading.Tasks;

namespace NBoosters.RedisBoost.Core.Pipeline
{
	internal interface IRedisPipeline
	{
		Task<RedisResponse> ExecuteCommandAsync(params byte[][] args);
		void ClosePipeline();
	}
}
