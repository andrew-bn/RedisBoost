using System;
using System.Threading.Tasks;
using NBoosters.RedisBoost.Core;

namespace NBoosters.RedisBoost
{
	public partial class RedisClient
	{
		public Task<string> DiscardAsync()
		{
			return StatusResponseCommand(RedisConstants.Discard);
		}
		public Task<byte[][]> ExecAsync()
		{
			return MultiBulkResponseCommand(RedisConstants.Exec);
		}
		public Task<string> MultiAsync()
		{
			return StatusResponseCommand(RedisConstants.Multi);
		}
		public Task<string> UnwatchAsync()
		{
			return StatusResponseCommand(RedisConstants.Unwatch);
		}
		public Task<string> WatchAsync(params string[] keys)
		{
			if (keys.Length == 0)
				throw new ArgumentException("Invalid keys count", "keys");

			var request = new byte[keys.Length + 1][];
			request[0] = RedisConstants.Watch;
			for (int i = 0; i < keys.Length; i++)
				request[i + 1] = ConvertToByteArray(keys[i]);

			return StatusResponseCommand(request);
		}
	}
}
