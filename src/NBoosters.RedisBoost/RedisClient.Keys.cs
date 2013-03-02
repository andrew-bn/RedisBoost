using System;
using System.Threading.Tasks;
using NBoosters.RedisBoost.Core;

namespace NBoosters.RedisBoost
{
	public partial class RedisClient
	{

		public Task<byte[][]> KeysAsync(string pattern)
		{
			return MultiBulkResponseCommand(RedisConstants.Keys, ConvertToByteArray(pattern));
		}
		public Task<long> DelAsync(string key)
		{
			return IntegerResponseCommand(RedisConstants.Del, ConvertToByteArray(key));
		}

		public Task<byte[]> DumpAsync(string key)
		{
			return BulkResponseCommand(RedisConstants.Dump, ConvertToByteArray(key));
		}

		public Task<string> RestoreAsync(string key, int ttlInMilliseconds, byte[] serializedValue)
		{
			return StatusResponseCommand(RedisConstants.Restore, ConvertToByteArray(key),
				ConvertToByteArray(ttlInMilliseconds), serializedValue);
		}

		public Task<long> ExistsAsync(string key)
		{
			return IntegerResponseCommand(RedisConstants.Exists, ConvertToByteArray(key));
		}

		public Task<long> ExpireAsync(string key, int seconds)
		{
			return IntegerResponseCommand(RedisConstants.Expire, ConvertToByteArray(key), ConvertToByteArray(seconds));
		}

		public Task<long> PExpireAsync(string key, int milliseconds)
		{
			return IntegerResponseCommand(RedisConstants.PExpire, ConvertToByteArray(key), ConvertToByteArray(milliseconds));
		}

		
		public Task<long> ExpireAtAsync(string key, DateTime timestamp)
		{
			var seconds = (int)(timestamp - RedisConstants.InitialUnixTime).TotalSeconds;
			return IntegerResponseCommand(RedisConstants.ExpireAt, ConvertToByteArray(key), 
				ConvertToByteArray(seconds));
		}
		public Task<long> PersistAsync(string key)
		{
			return IntegerResponseCommand(RedisConstants.Persist, ConvertToByteArray(key));
		}

		public Task<long> PttlAsync(string key)
		{
			return IntegerResponseCommand(RedisConstants.Pttl, ConvertToByteArray(key));
		}

		public Task<long> TtlAsync(string key)
		{
			return IntegerResponseCommand(RedisConstants.Ttl, ConvertToByteArray(key));
		}

		public Task<string> TypeAsync(string key)
		{
			return StatusResponseCommand(RedisConstants.Type, ConvertToByteArray(key));
		}

		public async Task<string> RandomKeyAsync()
		{
			var result = await BulkResponseCommand(RedisConstants.RandomKey).ConfigureAwait(false);
			return result != null && result.Length > 0
					   ? ConvertToString(result)
					   : String.Empty;
		}
		public Task<string> RenameAsync(string key, string newKey)
		{
			return StatusResponseCommand(RedisConstants.Rename, ConvertToByteArray(key), ConvertToByteArray(newKey));
		}
		public Task<long> RenameNxAsync(string key, string newKey)
		{
			return IntegerResponseCommand(RedisConstants.RenameNx, ConvertToByteArray(key), ConvertToByteArray(newKey));
		}
	}
}
