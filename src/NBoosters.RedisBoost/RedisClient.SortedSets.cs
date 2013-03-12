using System;
using System.Threading.Tasks;
using NBoosters.RedisBoost.Core;

namespace NBoosters.RedisBoost
{
	public partial class RedisClient
	{
		public Task<long> ZAddAsync<T>(string key, long score, T member)
		{
			return ZAddAsync(key, score, Serialize(member));
		}
		public Task<long> ZAddAsync(string key, long score, byte[] member)
		{
			return ZAddAsync(key, new ZAddArgs(score, member));
		}

		public Task<long> ZAddAsync<T>(string key, double score, T member)
		{
			return ZAddAsync(key, score, Serialize(member));
		}
		public Task<long> ZAddAsync(string key, double score, byte[] member)
		{
			return ZAddAsync(key, new ZAddArgs(score, member));
		}

		public Task<long> ZAddAsync(string key, params ZAddArgs[] args)
		{
			if (args.Length == 0)
				throw new ArgumentException("Invalid args count", "args");

			var request = new byte[2 + args.Length * 2][];
			request[0] = RedisConstants.ZAdd;
			request[1] = ConvertToByteArray(key);

			for (int i = 0; i < args.Length; i++)
			{
				var arg = args[i];
				request[i * 2 + 2] = arg.UseIntValue
									   ? ConvertToByteArray(arg.IntScore)
									   : ConvertToByteArray(arg.DoubleScore);
				request[i * 2 + 3] = arg.IsArray?(byte[])arg.Member:Serialize(arg.Member);
			}

			return IntegerResponseCommand(request);
		}

		public Task<long> ZRemAsync(string key, params object[] members)
		{
			return ZRemAsync(key, Serialize(members));
		}
		public Task<long> ZRemAsync<T>(string key, params T[] members)
		{
			return ZRemAsync(key, Serialize(members));
		}
		public Task<long> ZRemAsync(string key, params byte[][] members)
		{
			if (members.Length == 0)
				throw new ArgumentException("Invalid members count", "members");

			var request = new byte[2 + members.Length][];
			request[0] = RedisConstants.ZRem;
			request[1] = ConvertToByteArray(key);

			for (int i = 0; i < members.Length; i++)
				request[i + 2] = members[i];

			return IntegerResponseCommand(request);
		}
		public Task<long> ZRemRangeByRankAsync(string key, long start, long stop)
		{
			return IntegerResponseCommand(RedisConstants.ZRemRangeByRank,ConvertToByteArray(key),
				ConvertToByteArray(start),ConvertToByteArray(stop));
		}
		public Task<long> ZRemRangeByScoreAsync(string key, long min, long max)
		{
			return IntegerResponseCommand(RedisConstants.ZRemRangeByScore, ConvertToByteArray(key),
				ConvertToByteArray(min), ConvertToByteArray(max));
		}
		public Task<long> ZRemRangeByScoreAsync(string key, double min, double max)
		{
			return IntegerResponseCommand(RedisConstants.ZRemRangeByScore, ConvertToByteArray(key),
				ConvertToByteArray(min), ConvertToByteArray(max));
		}
		public Task<long> ZCardAsync(string key)
		{
			return IntegerResponseCommand(RedisConstants.ZCard, ConvertToByteArray(key));
		}
		public Task<long> ZCountAsync(string key, long min, long max)
		{
			return IntegerResponseCommand(RedisConstants.ZCount,
				ConvertToByteArray(key), ConvertToByteArray(min), ConvertToByteArray(max));
		}
		public Task<long> ZCountAsync(string key, double min, double max)
		{
			return IntegerResponseCommand(RedisConstants.ZCount, ConvertToByteArray(key), ConvertToByteArray(min), ConvertToByteArray(max));
		}
		public Task<double> ZIncrByAsync<T>(string key, long increment, T member)
		{
			return ZIncrByAsync(key, increment, Serialize(member));
		}
		public Task<double> ZIncrByAsync(string key, long increment, byte[] member)
		{
			return ZIncrByAsync(key, ConvertToByteArray(increment), member);
		}

		public Task<double> ZIncrByAsync<T>(string key, double increment, T member)
		{
			return ZIncrByAsync(key, increment, Serialize(member));
		}

		public Task<double> ZIncrByAsync(string key, double increment, byte[] member)
		{
			return ZIncrByAsync(key, ConvertToByteArray(increment), member);
		}
		private Task<double> ZIncrByAsync(string key, byte[] incrByValue, byte[] member)
		{
			return BulkResponseCommand(RedisConstants.ZIncrBy,
				ConvertToByteArray(key), incrByValue, member)
				.ContinueWith(t=> Deserialize<double>(t.Result));
		}
		public Task<long> ZInterStoreAsync(string destinationKey, params string[] keys)
		{
			if (keys.Length == 0)
				throw new ArgumentException("Invalid number of keys", "keys");

			var request = new byte[3 + keys.Length][];
			request[0] = RedisConstants.ZInterStore;
			request[1] = ConvertToByteArray(destinationKey);
			request[2] = ConvertToByteArray(keys.Length);

			for (int i = 0; i < keys.Length; i++)
				request[i + 3] = ConvertToByteArray(keys[i]);

			return IntegerResponseCommand(request);
		}
		public Task<long> ZInterStoreAsync(string destinationKey, Aggregation aggregation, params string[] keys)
		{
			if (keys.Length == 0)
				throw new ArgumentException("Invalid number of keys", "keys");

			var request = new byte[5 + keys.Length][];
			request[0] = RedisConstants.ZInterStore;
			request[1] = ConvertToByteArray(destinationKey);
			request[2] = ConvertToByteArray(keys.Length);

			for (int i = 0; i < keys.Length; i++)
				request[i + 3] = ConvertToByteArray(keys[i]);

			request[3 + keys.Length] = RedisConstants.Aggregate;
			request[3 + keys.Length + 1] = ConvertToByteArray(aggregation);

			return IntegerResponseCommand(request);
		}
		public Task<long> ZInterStoreAsync(string destinationKey, params ZAggrStoreArgs[] keys)
		{
			if (keys.Length == 0)
				throw new ArgumentException("Invalid number of keys", "keys");

			var request = new byte[4 + keys.Length * 2][];
			request[0] = RedisConstants.ZInterStore;
			request[1] = ConvertToByteArray(destinationKey);
			request[2] = ConvertToByteArray(keys.Length);

			for (int i = 0; i < keys.Length; i++)
				request[i + 3] = ConvertToByteArray(keys[i].Key);

			request[3 + keys.Length] = RedisConstants.Weights;

			for (int i = 0; i < keys.Length; i++)
				request[i + 4 + keys.Length] = ConvertToByteArray(keys[i].Weight);

			return IntegerResponseCommand(request);
		}
		public Task<long> ZInterStoreAsync(string destinationKey, Aggregation aggregation, params ZAggrStoreArgs[] keys)
		{
			if (keys.Length == 0)
				throw new ArgumentException("Invalid number of keys", "keys");

			var request = new byte[6 + keys.Length * 2][];
			request[0] = RedisConstants.ZInterStore;
			request[1] = ConvertToByteArray(destinationKey);
			request[2] = ConvertToByteArray(keys.Length);

			for (int i = 0; i < keys.Length; i++)
				request[i + 3] = ConvertToByteArray(keys[i].Key);

			request[3 + keys.Length] = RedisConstants.Weights;

			for (int i = 0; i < keys.Length; i++)
				request[i + 4 + keys.Length] = ConvertToByteArray(keys[i].Weight);

			request[4 + keys.Length * 2] = RedisConstants.Aggregate;
			request[5 + keys.Length * 2] = ConvertToByteArray(aggregation);

			return IntegerResponseCommand(request);
		}
		public Task<MultiBulk> ZRangeAsync(string key, long start, long stop, bool withScores = false)
		{
			return withScores
					? MultiBulkResponseCommand(RedisConstants.ZRange,
						ConvertToByteArray(key), ConvertToByteArray(start), ConvertToByteArray(stop), RedisConstants.WithScores)
					: MultiBulkResponseCommand(RedisConstants.ZRange,
						ConvertToByteArray(key), ConvertToByteArray(start), ConvertToByteArray(stop));
		}
		public Task<MultiBulk> ZRangeByScoreAsync(string key, long min, long max, bool withScores = false)
		{
			return ZRangeByScoreAsync(key, ConvertToByteArray(min), ConvertToByteArray(max), null, null, withScores);
		}
		public Task<MultiBulk> ZRangeByScoreAsync(string key, long min, long max,
			long limitOffset, long limitCount, bool withScores = false)
		{
			return ZRangeByScoreAsync(key, ConvertToByteArray(min), ConvertToByteArray(max),
				limitOffset, limitCount, withScores);
		}
		public Task<MultiBulk> ZRangeByScoreAsync(string key, double min, double max, bool withScores = false)
		{
			return ZRangeByScoreAsync(key, ConvertToByteArray(min), ConvertToByteArray(max), null, null, withScores);
		}
		public Task<MultiBulk> ZRangeByScoreAsync(string key, double min, double max,
			long limitOffset, long limitCount, bool withScores = false)
		{
			return ZRangeByScoreAsync(key, ConvertToByteArray(min), ConvertToByteArray(max),
				limitOffset, limitCount, withScores);
		}
		private Task<MultiBulk> ZRangeByScoreAsync(string key, byte[] min, byte[] max,
			long? limitOffset, long? limitCount, bool withscores = false)
		{
			if (limitOffset.HasValue && withscores)
				return MultiBulkResponseCommand(RedisConstants.ZRangeByScore, ConvertToByteArray(key),
											 min, max, RedisConstants.WithScores,
											 RedisConstants.Limit, ConvertToByteArray(limitOffset.Value),
											 ConvertToByteArray(limitCount.Value));
			if (limitOffset.HasValue)
				return MultiBulkResponseCommand(RedisConstants.ZRangeByScore, ConvertToByteArray(key),
											 min, max,
											 RedisConstants.Limit, ConvertToByteArray(limitOffset.Value),
											 ConvertToByteArray(limitCount.Value));
			if (withscores)
				return MultiBulkResponseCommand(RedisConstants.ZRangeByScore, ConvertToByteArray(key),
											 min, max, RedisConstants.WithScores);

			return MultiBulkResponseCommand(RedisConstants.ZRangeByScore, ConvertToByteArray(key), min, max);
		}

		public Task<long?> ZRankAsync<T>(string key, T member)
		{
			return ZRankAsync(key, Serialize(member));
		}

		public Task<long?> ZRankAsync(string key, byte[] member)
		{
			return IntegerOrBulkNullResponseCommand(RedisConstants.ZRank, ConvertToByteArray(key), member);
		}

		public Task<long?> ZRevRankAsync<T>(string key, T member)
		{
			return ZRevRankAsync(key, Serialize(member));
		}

		public Task<long?> ZRevRankAsync(string key, byte[] member)
		{
			return IntegerOrBulkNullResponseCommand(RedisConstants.ZRevRank, ConvertToByteArray(key), member);
		}
		public Task<MultiBulk> ZRevRangeAsync(string key, long start, long stop, bool withscores = false)
		{
			return withscores
				       ? MultiBulkResponseCommand(RedisConstants.ZRevRange, ConvertToByteArray(key),
				                               ConvertToByteArray(start), ConvertToByteArray(stop), RedisConstants.WithScores)
					   : MultiBulkResponseCommand(RedisConstants.ZRevRange, ConvertToByteArray(key),
				                               ConvertToByteArray(start), ConvertToByteArray(stop));
		}


		public Task<MultiBulk> ZRevRangeByScoreAsync(string key, long min, long max, bool withScores = false)
		{
			return ZRevRangeByScoreAsync(key, ConvertToByteArray(min), ConvertToByteArray(max), null, null, withScores);
		}
		public Task<MultiBulk> ZRevRangeByScoreAsync(string key, long min, long max,
			long limitOffset, long limitCount, bool withScores = false)
		{
			return ZRevRangeByScoreAsync(key, ConvertToByteArray(min), ConvertToByteArray(max),
				limitOffset, limitCount, withScores);
		}
		public Task<MultiBulk> ZRevRangeByScoreAsync(string key, double min, double max, bool withScores = false)
		{
			return ZRevRangeByScoreAsync(key, ConvertToByteArray(min), ConvertToByteArray(max), null, null, withScores);
		}
		public Task<MultiBulk> ZRevRangeByScoreAsync(string key, double min, double max,
			long limitOffset, long limitCount, bool withScores = false)
		{
			return ZRevRangeByScoreAsync(key, ConvertToByteArray(min), ConvertToByteArray(max),
				limitOffset, limitCount, withScores);
		}
		private Task<MultiBulk> ZRevRangeByScoreAsync(string key, byte[] min, byte[] max,
			long? limitOffset, long? limitCount, bool withscores = false)
		{
			if (limitOffset.HasValue && withscores)
				return MultiBulkResponseCommand(RedisConstants.ZRevRangeByScore, ConvertToByteArray(key),
											 min, max, RedisConstants.WithScores,
											 RedisConstants.Limit, ConvertToByteArray(limitOffset.Value),
											 ConvertToByteArray(limitCount.Value));
			if (limitOffset.HasValue)
				return MultiBulkResponseCommand(RedisConstants.ZRevRangeByScore, ConvertToByteArray(key),
											 min, max,
											 RedisConstants.Limit, ConvertToByteArray(limitOffset.Value),
											 ConvertToByteArray(limitCount.Value));
			if (withscores)
				return MultiBulkResponseCommand(RedisConstants.ZRevRangeByScore, ConvertToByteArray(key),
											 min, max, RedisConstants.WithScores);

			return MultiBulkResponseCommand(RedisConstants.ZRevRangeByScore, ConvertToByteArray(key), min, max);
		}

		public Task<double> ZScoreAsync<T>(string key, T member)
		{
			return ZScoreAsync(key, Serialize(member));
		}

		public Task<double> ZScoreAsync(string key, byte[] member)
		{
			return BulkResponseCommand(RedisConstants.ZScore, ConvertToByteArray(key), member)
					.ContinueWith(t=>Deserialize<double>(t.Result));
		}

		public Task<long> ZUnionStoreAsync(string destinationKey, params string[] keys)
		{
			if (keys.Length == 0)
				throw new ArgumentException("Invalid number of keys", "keys");

			var request = new byte[3 + keys.Length][];
			request[0] = RedisConstants.ZUnionStore;
			request[1] = ConvertToByteArray(destinationKey);
			request[2] = ConvertToByteArray(keys.Length);

			for (int i = 0; i < keys.Length; i++)
				request[i + 3] = ConvertToByteArray(keys[i]);

			return IntegerResponseCommand(request);
		}
		public Task<long> ZUnionStoreAsync(string destinationKey, Aggregation aggregation, params string[] keys)
		{
			if (keys.Length == 0)
				throw new ArgumentException("Invalid number of keys", "keys");

			var request = new byte[5 + keys.Length][];
			request[0] = RedisConstants.ZUnionStore;
			request[1] = ConvertToByteArray(destinationKey);
			request[2] = ConvertToByteArray(keys.Length);

			for (int i = 0; i < keys.Length; i++)
				request[i + 3] = ConvertToByteArray(keys[i]);

			request[3 + keys.Length] = RedisConstants.Aggregate;
			request[3 + keys.Length + 1] = ConvertToByteArray(aggregation);

			return IntegerResponseCommand(request);
		}
		public Task<long> ZUnionStoreAsync(string destinationKey, params ZAggrStoreArgs[] keys)
		{
			if (keys.Length == 0)
				throw new ArgumentException("Invalid number of keys", "keys");

			var request = new byte[4 + keys.Length * 2][];
			request[0] = RedisConstants.ZUnionStore;
			request[1] = ConvertToByteArray(destinationKey);
			request[2] = ConvertToByteArray(keys.Length);

			for (int i = 0; i < keys.Length; i++)
				request[i + 3] = ConvertToByteArray(keys[i].Key);

			request[3 + keys.Length] = RedisConstants.Weights;

			for (int i = 0; i < keys.Length; i++)
				request[i + 4 + keys.Length] = ConvertToByteArray(keys[i].Weight);

			return IntegerResponseCommand(request);
		}
		public Task<long> ZUnionStoreAsync(string destinationKey, Aggregation aggregation, params ZAggrStoreArgs[] keys)
		{
			if (keys.Length == 0)
				throw new ArgumentException("Invalid number of keys", "keys");

			var request = new byte[6 + keys.Length * 2][];
			request[0] = RedisConstants.ZUnionStore;
			request[1] = ConvertToByteArray(destinationKey);
			request[2] = ConvertToByteArray(keys.Length);

			for (int i = 0; i < keys.Length; i++)
				request[i + 3] = ConvertToByteArray(keys[i].Key);

			request[3 + keys.Length] = RedisConstants.Weights;

			for (int i = 0; i < keys.Length; i++)
				request[i + 4 + keys.Length] = ConvertToByteArray(keys[i].Weight);

			request[4 + keys.Length * 2] = RedisConstants.Aggregate;
			request[5 + keys.Length * 2] = ConvertToByteArray(aggregation);

			return IntegerResponseCommand(request);
		}
	}
}
