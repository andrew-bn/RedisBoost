using System.Threading.Tasks;
using NBoosters.RedisBoost.Core;

namespace NBoosters.RedisBoost
{
	public partial class RedisClient
	{
		public Task<MultiBulk> BlPopAsync(int timeoutInSeconds, params string[] keys)
		{
			var request = new byte[keys.Length+2][];
			request[0] = RedisConstants.BlPop;

			for (int i = 0; i < keys.Length; i++)
				request[i + 1] = ConvertToByteArray(keys[i]);

			request[request.Length - 1] = ConvertToByteArray(timeoutInSeconds);

			return MultiBulkResponseCommand(request);
		}
		public Task<long> LPushAsync(string key, params byte[][] values)
		{
			var request = new byte[values.Length + 2][];
			request[0] = RedisConstants.LPush;
			request[1] = ConvertToByteArray(key);
			for (int i = 0; i < values.Length; i++)
				request[i + 2] = values[i];

			return IntegerResponseCommand(request);
		}

		public Task<MultiBulk> BrPopAsync(int timeoutInSeconds, params string[] keys)
		{
			var request = new byte[keys.Length + 2][];
			request[0] = RedisConstants.BrPop;

			for (int i = 0; i < keys.Length; i++)
				request[i + 1] = ConvertToByteArray(keys[i]);

			request[request.Length - 1] = ConvertToByteArray(timeoutInSeconds);

			return MultiBulkResponseCommand(request);
		}
		public Task<long> RPushAsync(string key, params byte[][] values)
		{
			var request = new byte[values.Length + 2][];
			request[0] = RedisConstants.RPush;
			request[1] = ConvertToByteArray(key);
			for (int i = 0; i < values.Length; i++)
				request[i + 2] = values[i];

			return IntegerResponseCommand(request);
		}

		public Task<byte[]> LPopAsync(string key)
		{
			return BulkResponseCommand(RedisConstants.LPop, ConvertToByteArray(key));
		}
		public Task<byte[]> RPopAsync(string key)
		{
			return BulkResponseCommand(RedisConstants.RPop, ConvertToByteArray(key));
		}
		public Task<byte[]> RPopLPushAsync(string source,string destination)
		{
			return BulkResponseCommand(RedisConstants.RPopLPush, ConvertToByteArray(source),
				ConvertToByteArray(destination));
		}
		public Task<byte[]> BRPopLPushAsync(string sourceKey, string destinationKey,int timeoutInSeconds)
		{
			return BulkResponseCommand(RedisConstants.BRPopLPush, ConvertToByteArray(sourceKey),
				ConvertToByteArray(destinationKey),ConvertToByteArray(timeoutInSeconds));
		}
		public Task<byte[]> LIndexAsync(string key, int index)
		{
			return BulkResponseCommand(RedisConstants.LIndex, ConvertToByteArray(key), ConvertToByteArray(index));
		}
		public Task<long> LInsertAsync(string key, byte[] pivot, byte[] value, bool before = true)
		{
			return IntegerResponseCommand(RedisConstants.LInsert,
				ConvertToByteArray(key),before?RedisConstants.Before:RedisConstants.After,
				pivot, value);
		}
		public Task<long> LLenAsync(string key)
		{
			return IntegerResponseCommand(RedisConstants.LLen, ConvertToByteArray(key));
		}
		public Task<long> LPushXAsync(string key, byte[] values)
		{
			return IntegerResponseCommand(RedisConstants.LPushX, ConvertToByteArray(key), values);
		}
		public Task<MultiBulk> LRangeAsync(string key, int start, int stop)
		{
			return MultiBulkResponseCommand(RedisConstants.LRange,
				ConvertToByteArray(key), ConvertToByteArray(start), ConvertToByteArray(stop));
		}
		public Task<long> LRemAsync(string key, int count, byte[] value)
		{
			return IntegerResponseCommand(RedisConstants.LRem,
				ConvertToByteArray(key), ConvertToByteArray(count), value);
		}
		public Task<string> LSetAsync(string key, int index, byte[] value)
		{
			return StatusResponseCommand(RedisConstants.LSet,
				ConvertToByteArray(key), ConvertToByteArray(index), value);
		}
		public Task<string> LTrimAsync(string key, int start, int stop)
		{
			return StatusResponseCommand(RedisConstants.LTrim,
				ConvertToByteArray(key), ConvertToByteArray(start), ConvertToByteArray(stop));
		}
		public Task<long> RPushXAsync(string key, byte[] values)
		{
			return IntegerResponseCommand(RedisConstants.RPushX, ConvertToByteArray(key), values);
		}
	}
}
