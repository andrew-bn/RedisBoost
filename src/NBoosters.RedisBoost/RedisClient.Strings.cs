using System;
using System.Linq;
using System.Threading.Tasks;
using NBoosters.RedisBoost.Core;

namespace NBoosters.RedisBoost
{
	public partial class RedisClient
	{
		public Task<Bulk> GetAsync(string key)
		{
			return BulkResponseCommand(RedisConstants.Get, ConvertToByteArray(key));
		}

		public Task<string> SetAsync<T>(string key, T value)
		{
			return SetAsync(key, Serialize(value));
		}
		public Task<string> SetAsync(string key, byte[] value)
		{
			return StatusResponseCommand(RedisConstants.Set, ConvertToByteArray(key), value);
		}

		public Task<long> AppendAsync<T>(string key, T value)
		{
			return AppendAsync(key, Serialize(value));
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

		public Task<Bulk> IncrByFloatAsync(string key, double increment)
		{
			return BulkResponseCommand(RedisConstants.IncrByFloat, ConvertToByteArray(key),
				ConvertToByteArray(increment));
		}

		public Task<T> GetRangeAsync<T>(string key, int start, int end)
		{
			return GetRangeAsync(key, start,end).ContinueWith(t => Deserialize<T>(t.Result));
		}
		public Task<Bulk> GetRangeAsync(string key, int start, int end)
		{
			return BulkResponseCommand(RedisConstants.GetRange, ConvertToByteArray(key),
				ConvertToByteArray(start),ConvertToByteArray(end));
		}

		public Task<long> SetRangeAsync<T>(string key, int offset, T value)
		{
			return SetRangeAsync(key, offset, Serialize(value));
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

		public Task<Bulk> GetSetAsync<T>(string key, T value)
		{
			return GetSetAsync(key, Serialize(value));
		}
		public Task<Bulk> GetSetAsync(string key, byte[] value)
		{
			return BulkResponseCommand(RedisConstants.GetSet, ConvertToByteArray(key), value);
		}

		public Task<MultiBulk> MGetAsync(params string[] keys)
		{
			if (keys.Length == 0)
				throw new ArgumentException("Invalid argument items count", "keys");
			var request = new byte[keys.Length+1][];
			request[0] = RedisConstants.MGet;

			for (int i = 0; i < keys.Length; i++)
				request[i + 1] = ConvertToByteArray(keys[i]);

			return MultiBulkResponseCommand(request);
		}
		public Task<string> MSetAsync(params MSetArgs[] args)
		{
			var request = ComposeMSetRequest(RedisConstants.MSet, args);
			return StatusResponseCommand(request);
		}
		public Task<long> MSetNxAsync(params MSetArgs[] args)
		{
			var request = ComposeMSetRequest(RedisConstants.MSetNx, args);
			return IntegerResponseCommand(request);
		}

		public Task<string> SetExAsync<T>(string key, int seconds, T value)
		{
			return SetExAsync(key, seconds, Serialize(value));
		}
		public Task<string> SetExAsync(string key, int seconds, byte[] value)
		{
			return StatusResponseCommand(RedisConstants.SetEx, ConvertToByteArray(key),
				ConvertToByteArray(seconds), value);
		}

		public Task<string> PSetExAsync<T>(string key, int milliseconds, T value)
		{
			return PSetExAsync(key, milliseconds, Serialize(value));
		}
		public Task<string> PSetExAsync(string key, int milliseconds, byte[] value)
		{
			return StatusResponseCommand(RedisConstants.PSetEx, ConvertToByteArray(key),
				ConvertToByteArray(milliseconds), value);
		}

		public Task<long> SetNxAsync<T>(string key, T value)
		{
			return SetNxAsync(key, Serialize(value));
		}
		public Task<long> SetNxAsync(string key, byte[] value)
		{
			return IntegerResponseCommand(RedisConstants.SetNx, ConvertToByteArray(key), value);
		}

		public Task<long> BitOpAsync(BitOpType bitOp, string destKey, params string[] keys)
		{
			if (keys.Length == 0)
				throw new ArgumentException("Invalid argument items count", "keys");

			var request = new byte[3 + keys.Length][];
			request[0] = RedisConstants.BitOp;
			request[1] = ConvertToByteArray(bitOp);
			request[2] = ConvertToByteArray(destKey);
			for (int i = 0; i < keys.Length; i++)
				request[3 + i] = ConvertToByteArray(keys[i]);

			return IntegerResponseCommand(request);
		}
		public Task<long> GetBitAsync(string key, long offset)
		{
			return IntegerResponseCommand(RedisConstants.GetBit,ConvertToByteArray(key),
				ConvertToByteArray(offset));
		}
		public Task<long> SetBitAsync(string key, long offset, int value)
		{
			return IntegerResponseCommand(RedisConstants.SetBit, ConvertToByteArray(key),
				ConvertToByteArray(offset), ConvertToByteArray(value));
		}

		private byte[][] ComposeMSetRequest(byte[] command, MSetArgs[] args)
		{
			if (args.Length == 0)
				throw new ArgumentException("Invalid argument items count", "args");

			var request = new byte[args.Length * 2 + 1][];
			request[0] = command;
			for (int i = 0; i < args.Length; i++)
			{
				var arg = args[i];
				request[i * 2 + 1] = ConvertToByteArray(arg.KeyOrField);
				request[i * 2 + 2] = arg.IsArray ? (byte[])arg.Value : Serialize(arg.Value);
			}
			return request;
		}
	}
}
