using System.Linq;
using System.Threading.Tasks;
using NBoosters.RedisBoost.Core;

namespace NBoosters.RedisBoost
{
	public partial class RedisClient
	{
		public Task<long> HSetAsync(string key, string field, byte[] value)
		{
			return HSetAsync(key, ConvertToByteArray(field), value);
		}

		public Task<long> HSetAsync(string key, byte[] field, byte[] value)
		{
			return IntegerResponseCommand(RedisConstants.HSet, ConvertToByteArray(key),field,value);
		}
		public Task<long> HSetNxAsync(string key, string field, byte[] value)
		{
			return HSetNxAsync(key, ConvertToByteArray(field), value);
		}

		public Task<long> HSetNxAsync(string key, byte[] field, byte[] value)
		{
			return IntegerResponseCommand(RedisConstants.HSetNx, ConvertToByteArray(key), field, value);
		}
		public Task<long> HExistsAsync(string key, string field)
		{
			return HExistsAsync(key, ConvertToByteArray(field));
		}

		public Task<long> HExistsAsync(string key, byte[] field)
		{
			return IntegerResponseCommand(RedisConstants.HExists, ConvertToByteArray(key), field);
		}

		public Task<long> HDelAsync(string key, params string[] fields)
		{
			return HDelAsync(key, fields.Select(ConvertToByteArray).ToArray());
		}

		public Task<long> HDelAsync(string key, params byte[][] fields)
		{
			if (fields.Length == 0)
				throw new RedisException("Fields array is empty");

			var request = new byte[fields.Length + 2][];
			request[0] = RedisConstants.HDel;
			request[1] = ConvertToByteArray(key);
			for (int i = 0; i < fields.Length; i++)
				request[i + 2] = fields[i];
			return IntegerResponseCommand(request);
		}

		public Task<byte[]> HGetAsync(string key, string field)
		{
			return HGetAsync(key, ConvertToByteArray(field));
		}

		public Task<byte[]> HGetAsync(string key, byte[] field)
		{
			return BulkResponseCommand(RedisConstants.HGet, ConvertToByteArray(key), field);
		}

		public Task<byte[][]> HGetAllAsync(string key)
		{
			return MultiBulkResponseCommand(RedisConstants.HGetAll, ConvertToByteArray(key));
		}

		public Task<long> HIncrByAsync(string key, string field, int increment)
		{
			return HIncrByAsync(key, ConvertToByteArray(field), increment);
		}
		public Task<long> HIncrByAsync(string key, byte[] field, int increment)
		{
			return IntegerResponseCommand(RedisConstants.HIncrBy, ConvertToByteArray(key),
				field,ConvertToByteArray(increment));
		}

		public Task<byte[][]> HKeysAsync(string key)
		{
			return MultiBulkResponseCommand(RedisConstants.HKeys, ConvertToByteArray(key));
		}
		public Task<byte[][]> HValsAsync(string key)
		{
			return MultiBulkResponseCommand(RedisConstants.HVals, ConvertToByteArray(key));
		}
		public Task<long> HLenAsync(string key)
		{
			return IntegerResponseCommand(RedisConstants.HLen, ConvertToByteArray(key));
		}

		public Task<byte[][]> HMGetAsync(string key, params string[] fields)
		{
			return HMGetAsync(key, fields.Select(ConvertToByteArray).ToArray());
		}

		public Task<byte[][]> HMGetAsync(string key, params byte[][] fields)
		{
			if (fields.Length == 0)
				throw new RedisException("Invalid argument 'fields'");
			var request = new byte[fields.Length + 2][];
			request[0] = RedisConstants.HMGet;
			request[1] = ConvertToByteArray(key);
			for (int i = 0; i < fields.Length; i++)
				request[i + 2] = fields[i];

			return MultiBulkResponseCommand(request);
		}
		public Task<string> HMSetAsync(string key,params MSetArgs[] args)
		{
			if (args.Length == 0)
				throw new RedisException("Invalid argument 'args'");

			var request = new byte[args.Length * 2 + 2][];
			request[0] = RedisConstants.HMSet;
			request[1] = ConvertToByteArray(key);
			for (int i = 0; i < args.Length; i++)
			{
				request[i * 2 + 2] = ConvertToByteArray(args[i].KeyOrField);
				request[i * 2 + 3] = args[i].Value;
			}
			return StatusResponseCommand(request);
		}
	}
}
