using System.Threading.Tasks;
using RedisBoost.Core;
using RedisBoost.Misk;

namespace RedisBoost
{
	public partial class RedisClient
	{
		public Task<long> PfAddAsync(string key, params object[] values)
		{
			return PfAddAsync(key, Serialize(values));
		}

		public Task<long> PfAddAsync<T>(string key, params T[] values)
		{
			return PfAddAsync(key, Serialize(values));
		}

		public Task<long> PfAddAsync(string key, params byte[][] values)
		{
			var request = ComposeRequest(RedisConstants.PfAdd, key.ToBytes(), values);
			return IntegerCommand(request);
		}

		public Task<long> PfCountAsync(params string[] keys)
		{
			var request = ComposeRequest(RedisConstants.PfCount, keys);
			return IntegerCommand(request);
		}

		public Task<string> PfMergeAsync(string destKey, params string[] srcKeys)
		{
			var request = ComposeRequest(RedisConstants.PfMerge, destKey.ToBytes(), srcKeys);
			return StatusCommand(request);
		}
	}
}
