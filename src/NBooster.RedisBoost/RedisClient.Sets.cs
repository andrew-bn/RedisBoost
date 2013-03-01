using System;
using System.Threading.Tasks;
using NBooster.RedisBoost.Core;

namespace NBooster.RedisBoost
{
	public partial class RedisClient
	{
		public Task<long> SAddAsync(string key, params byte[][] member)
		{
			if (member.Length == 0)
				throw new ArgumentException("Invalid members count", "member");

			var request = new byte[2 + member.Length][];
			request[0] = RedisConstants.SAdd;
			request[1] = ConvertToByteArray(key);

			for (int i = 0; i < member.Length; i++)
				request[i + 2] = member[i];

			return IntegerResponseCommand(request);
		}
		public Task<long> SRemAsync(string key, params byte[][] member)
		{
			if (member.Length == 0)
				throw new ArgumentException("Invalid members count","member");

			var request = new byte[2 + member.Length][];
			request[0] = RedisConstants.SRem;
			request[1] = ConvertToByteArray(key);

			for (int i = 0; i < member.Length; i++)
				request[i + 2] = member[i];

			return IntegerResponseCommand(request);
		}
		public Task<long> SCardAsync(string key)
		{
			return IntegerResponseCommand(RedisConstants.SCard, ConvertToByteArray(key));
		}
		public Task<byte[][]> SDiffAsync(params string[] keys)
		{
			if (keys.Length == 0)
				throw new ArgumentException("Invalid number of keys", "keys");

			var request = new byte[keys.Length + 1][];
			request[0] = RedisConstants.SDiff;
			for (int i = 0; i < keys.Length; i++)
				request[i + 1] = ConvertToByteArray(keys[i]);

			return MultiBulkResponseCommand(request);
		}
		public Task<long> SDiffStoreAsync(string destinationKey, params string[] keys)
		{
			if (keys.Length == 0)
				throw new ArgumentException("Invalid number of keys", "keys");

			var request = new byte[keys.Length + 2][];
			request[0] = RedisConstants.SDiffStore;
			request[1] = ConvertToByteArray(destinationKey);
			for (int i = 0; i < keys.Length; i++)
				request[i + 2] = ConvertToByteArray(keys[i]);

			return IntegerResponseCommand(request);
		}
		public Task<byte[][]> SUnionAsync(params string[] keys)
		{
			if (keys.Length == 0)
				throw new ArgumentException("Invalid number of keys", "keys");

			var request = new byte[keys.Length + 1][];
			request[0] = RedisConstants.SUnion;
			for (int i = 0; i < keys.Length; i++)
				request[i + 1] = ConvertToByteArray(keys[i]);

			return MultiBulkResponseCommand(request);
		}
		public Task<long> SUnionStoreAsync(string destinationKey, params string[] keys)
		{
			if (keys.Length == 0)
				throw new ArgumentException("Invalid number of keys", "keys");

			var request = new byte[keys.Length + 2][];
			request[0] = RedisConstants.SUnionStore;
			request[1] = ConvertToByteArray(destinationKey);
			for (int i = 0; i < keys.Length; i++)
				request[i + 2] = ConvertToByteArray(keys[i]);

			return IntegerResponseCommand(request);
		}
		public Task<byte[][]> SInterAsync(params string[] keys)
		{
			if (keys.Length == 0)
				throw new ArgumentException("Invalid number of keys", "keys");

			var request = new byte[keys.Length + 1][];
			request[0] = RedisConstants.SInter;
			for (int i = 0; i < keys.Length; i++)
				request[i + 1] = ConvertToByteArray(keys[i]);

			return MultiBulkResponseCommand(request);
		}
		public Task<long> SInterStoreAsync(string destinationKey, params string[] keys)
		{
			if (keys.Length == 0)
				throw new ArgumentException("Invalid number of keys", "keys");

			var request = new byte[keys.Length + 2][];
			request[0] = RedisConstants.SInterStore;
			request[1] = ConvertToByteArray(destinationKey);
			for (int i = 0; i < keys.Length; i++)
				request[i + 2] = ConvertToByteArray(keys[i]);

			return IntegerResponseCommand(request);
		}
		public Task<long> SIsMemberAsync(string key, byte[] value)
		{
			return IntegerResponseCommand(RedisConstants.SIsMember, ConvertToByteArray(key), value);
		}
		public Task<byte[][]> SMembersAsync(string key)
		{
			return MultiBulkResponseCommand(RedisConstants.SMembers, ConvertToByteArray(key));
		}
		public Task<long> SMoveAsync(string sourceKey, string destinationKey, byte[] member)
		{
			return IntegerResponseCommand(RedisConstants.SMove, ConvertToByteArray(sourceKey),
				ConvertToByteArray(destinationKey), member);
		}
		public Task<byte[]> SPopAsync(string key)
		{
			return BulkResponseCommand(RedisConstants.SPop, ConvertToByteArray(key));
		}

		public Task<byte[]> SRandMemberAsync(string key)
		{
			return BulkResponseCommand(RedisConstants.SRandMember, ConvertToByteArray(key));
		}
		public Task<byte[][]> SRandMemberAsync(string key, int count)
		{
			return MultiBulkResponseCommand(RedisConstants.SRandMember, ConvertToByteArray(key),
				ConvertToByteArray(count));
		}
	}
}
