using System.Threading.Tasks;
using NBooster.RedisBoost.Core;

namespace NBooster.RedisBoost
{
	public partial class RedisClient
	{
		public Task<byte[]> GetAsync(string key)
		{
			return BulkResponseCommand(RedisConstants.Get, ConvertToByteArray(key));
		}
		public Task<string> SetAsync(string key, byte[] value)
		{
			return StatusResponseCommand(RedisConstants.Set, ConvertToByteArray(key), value);
		}
		public Task<long> AppendAsync(string key, byte[] value)
		{
			return IntegerResponseCommand(RedisConstants.Append, ConvertToByteArray(key), value);
		}
		public Task<long> BitCountAsync(string key)
		{
			return IntegerResponseCommand(RedisConstants.BitCount);
		}
		public Task<long> BitCountAsync(string key, int start, int end)
		{
			return IntegerResponseCommand(RedisConstants.BitCount,ConvertToByteArray(start),
				ConvertToByteArray(end));
		}
		public Task<long> DecrAsync(string key)
		{
			return IntegerResponseCommand(RedisConstants.Decr,ConvertToByteArray(key));
		}
		public Task<long> IncrAsync(string key)
		{
			return IntegerResponseCommand(RedisConstants.Incr,ConvertToByteArray(key));
		}
		public Task<long> DecrByAsync(string key, int decrement)
		{
			return IntegerResponseCommand(RedisConstants.DecrBy, ConvertToByteArray(key),
				ConvertToByteArray(decrement));
		}
		public Task<long> IncrByAsync(string key, int increment)
		{
			return IntegerResponseCommand(RedisConstants.IncrBy, ConvertToByteArray(key),
				ConvertToByteArray(increment));
		}
		public Task<byte[]> GetRangeAsync(string key, int start, int end)
		{
			return BulkResponseCommand(RedisConstants.GetRange, ConvertToByteArray(key),
				ConvertToByteArray(start),ConvertToByteArray(end));
		}
		public Task<long> SetRangeAsync(string key, int offset, byte[] value)
		{
			return IntegerResponseCommand(RedisConstants.SetRange, ConvertToByteArray(key),
				ConvertToByteArray(offset), value);
		}
		public Task<long> StrLenAsync(string key)
		{
			return IntegerResponseCommand(RedisConstants.StrLen, ConvertToByteArray(key));
		}
		public Task<byte[]> GetSetAsync(string key, byte[] value)
		{
			return BulkResponseCommand(RedisConstants.GetSet, ConvertToByteArray(key), value);
		}
		public Task<byte[][]> MGetAsync(params string[] key)
		{
			if (key.Length == 0)
				throw new RedisException("Invalid argument 'key'");
			var request = new byte[key.Length+1][];
			request[0] = RedisConstants.MGet;

			for (int i = 0; i < key.Length; i++)
				request[i + 1] = ConvertToByteArray(key[i]);

			return MultiBulkResponseCommand(request);
		}
		public Task<string> MSetAsync(params MSetArgs[] args)
		{
			if (args.Length == 0)
				throw new RedisException("Invalid argument 'args'");

			var request = new byte[args.Length * 2+1][];
			request[0] = RedisConstants.MSet;
			for (int i = 0; i < args.Length; i++)
			{
				request[i*2 + 1] = ConvertToByteArray(args[i].KeyOrField);
				request[i*2 + 2] = args[i].Value;
			}
			return StatusResponseCommand(request);
		}
		public Task<long> MSetNxAsync(params MSetArgs[] args)
		{
			if (args.Length == 0)
				throw new RedisException("Invalid argument 'args'");

			var request = new byte[args.Length * 2 + 1][];
			request[0] = RedisConstants.MSetNx;
			for (int i = 0; i < args.Length; i++)
			{
				request[i * 2 + 1] = ConvertToByteArray(args[i].KeyOrField);
				request[i * 2 + 2] = args[i].Value;
			}
			return IntegerResponseCommand(request);
		}
		public Task<string> SetExAsync(string key, int seconds, byte[] value)
		{
			return StatusResponseCommand(RedisConstants.SetEx, ConvertToByteArray(key),
				ConvertToByteArray(seconds), value);
		}
		public Task<string> PSetExAsync(string key, int milliseconds, byte[] value)
		{
			return StatusResponseCommand(RedisConstants.PSetEx, ConvertToByteArray(key),
				ConvertToByteArray(milliseconds), value);
		}
		public Task<long> SetNxAsync(string key, byte[] value)
		{
			return IntegerResponseCommand(RedisConstants.SetNx, ConvertToByteArray(key), value);
		}
	}
}
