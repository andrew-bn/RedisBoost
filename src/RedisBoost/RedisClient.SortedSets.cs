#region Apache Licence, Version 2.0
/*
 Copyright 2015 Andrey Bulygin.

 Licensed under the Apache License, Version 2.0 (the "License"); 
 you may not use this file except in compliance with the License. 
 You may obtain a copy of the License at 

		http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software 
 distributed under the License is distributed on an "AS IS" BASIS, 
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
 See the License for the specific language governing permissions 
 and limitations under the License.
 */
#endregion

using System;
using System.Threading.Tasks;
using RedisBoost.Misk;
using RedisBoost.Core;

namespace RedisBoost
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
			var request = ComposeRequest(RedisConstants.ZAdd, key.ToBytes(), args);
			return IntegerCommand(request);
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
			var request = ComposeRequest(RedisConstants.ZRem, key.ToBytes(), members);
			return IntegerCommand(request);
		}

		public Task<long> ZRemRangeByRankAsync(string key, long start, long stop)
		{
			return IntegerCommand(RedisConstants.ZRemRangeByRank, key.ToBytes(), start.ToBytes(), stop.ToBytes());
		}

		public Task<long> ZRemRangeByScoreAsync(string key, long min, long max)
		{
			return IntegerCommand(RedisConstants.ZRemRangeByScore, key.ToBytes(), min.ToBytes(), max.ToBytes());
		}

		public Task<long> ZRemRangeByScoreAsync(string key, double min, double max)
		{
			return IntegerCommand(RedisConstants.ZRemRangeByScore, key.ToBytes(), min.ToBytes(), max.ToBytes());
		}

		public Task<long> ZCardAsync(string key)
		{
			return IntegerCommand(RedisConstants.ZCard, key.ToBytes());
		}

		public Task<long> ZCountAsync(string key, long min, long max)
		{
			return IntegerCommand(RedisConstants.ZCount, key.ToBytes(), min.ToBytes(), max.ToBytes());
		}

		public Task<long> ZCountAsync(string key, double min, double max)
		{
			return IntegerCommand(RedisConstants.ZCount, key.ToBytes(), min.ToBytes(), max.ToBytes());
		}

		public Task<double> ZIncrByAsync<T>(string key, long increment, T member)
		{
			return ZIncrByAsync(key, increment, Serialize(member));
		}

		public Task<double> ZIncrByAsync(string key, long increment, byte[] member)
		{
			return ZIncrByAsync(key, increment.ToBytes(), member);
		}

		public Task<double> ZIncrByAsync<T>(string key, double increment, T member)
		{
			return ZIncrByAsync(key, increment, Serialize(member));
		}

		public Task<double> ZIncrByAsync(string key, double increment, byte[] member)
		{
			return ZIncrByAsync(key, increment.ToBytes(), member);
		}

		private Task<double> ZIncrByAsync(string key, byte[] incrByValue, byte[] member)
		{
			return BulkCommand(RedisConstants.ZIncrBy, key.ToBytes(), incrByValue, member)
					.ContinueWithIfNoError(t => Deserialize<double>(t.Result));
		}

		public Task<long> ZInterStoreAsync(string destinationKey, params string[] keys)
		{
			var request = ComposeRequest(RedisConstants.ZInterStore, destinationKey.ToBytes(), keys.Length.ToBytes(), keys);
			return IntegerCommand(request);
		}

		public Task<long> ZInterStoreAsync(string destinationKey, Aggregation aggregation, params string[] keys)
		{
			var request = ComposeRequest(RedisConstants.ZInterStore, destinationKey.ToBytes(),
										 keys.Length.ToBytes(), keys, RedisConstants.Aggregate,
										 aggregation.ToBytes());
			return IntegerCommand(request);
		}
		public Task<long> ZInterStoreAsync(string destinationKey, params ZAggrStoreArgs[] keys)
		{
			var request = ComposeRequest(RedisConstants.ZInterStore, destinationKey.ToBytes(),
										 keys.Length.ToBytes(), keys, RedisConstants.Weights);
			return IntegerCommand(request);
		}
		public Task<long> ZInterStoreAsync(string destinationKey, Aggregation aggregation, params ZAggrStoreArgs[] keys)
		{
			var request = ComposeRequest(RedisConstants.ZInterStore, destinationKey.ToBytes(),
										 keys.Length.ToBytes(), keys, RedisConstants.Weights, RedisConstants.Aggregate,
										 aggregation.ToBytes());
			return IntegerCommand(request);
		}
		public Task<MultiBulk> ZRangeAsync(string key, long start, long stop, bool withScores = false)
		{
			return withScores
					? MultiBulkCommand(RedisConstants.ZRange,
						key.ToBytes(), start.ToBytes(), stop.ToBytes(), RedisConstants.WithScores)
					: MultiBulkCommand(RedisConstants.ZRange,
						key.ToBytes(), start.ToBytes(), stop.ToBytes());
		}
		public Task<MultiBulk> ZRangeByScoreAsync(string key, long min, long max, bool withScores = false)
		{
			return ZRangeByScoreAsync(key, min.ToBytes(), max.ToBytes(), null, null, withScores);
		}
		public Task<MultiBulk> ZRangeByScoreAsync(string key, long min, long max,
			long limitOffset, long limitCount, bool withScores = false)
		{
			return ZRangeByScoreAsync(key, min.ToBytes(), max.ToBytes(),
				limitOffset, limitCount, withScores);
		}
		public Task<MultiBulk> ZRangeByScoreAsync(string key, double min, double max, bool withScores = false)
		{
			return ZRangeByScoreAsync(key, min.ToBytes(), max.ToBytes(), null, null, withScores);
		}
		public Task<MultiBulk> ZRangeByScoreAsync(string key, double min, double max, long limitOffset, long limitCount, bool withScores = false)
		{
			return ZRangeByScoreAsync(key, min.ToBytes(), max.ToBytes(), limitOffset, limitCount, withScores);
		}
		private Task<MultiBulk> ZRangeByScoreAsync(string key, byte[] min, byte[] max, long? limitOffset, long? limitCount, bool withscores = false)
		{
			if (limitOffset.HasValue && withscores)
				return MultiBulkCommand(RedisConstants.ZRangeByScore, key.ToBytes(),
											 min, max, RedisConstants.WithScores,
											 RedisConstants.Limit, limitOffset.Value.ToBytes(),
											 limitCount.Value.ToBytes());
			if (limitOffset.HasValue)
				return MultiBulkCommand(RedisConstants.ZRangeByScore, key.ToBytes(),
											 min, max,
											 RedisConstants.Limit, limitOffset.Value.ToBytes(),
											 limitCount.Value.ToBytes());
			if (withscores)
				return MultiBulkCommand(RedisConstants.ZRangeByScore, key.ToBytes(),
											 min, max, RedisConstants.WithScores);

			return MultiBulkCommand(RedisConstants.ZRangeByScore, key.ToBytes(), min, max);
		}

		public Task<long?> ZRankAsync<T>(string key, T member)
		{
			return ZRankAsync(key, Serialize(member));
		}

		public Task<long?> ZRankAsync(string key, byte[] member)
		{
			return IntegerOrBulkNullCommand(RedisConstants.ZRank, key.ToBytes(), member);
		}

		public Task<long?> ZRevRankAsync<T>(string key, T member)
		{
			return ZRevRankAsync(key, Serialize(member));
		}

		public Task<long?> ZRevRankAsync(string key, byte[] member)
		{
			return IntegerOrBulkNullCommand(RedisConstants.ZRevRank, key.ToBytes(), member);
		}

		public Task<MultiBulk> ZRevRangeAsync(string key, long start, long stop, bool withscores = false)
		{
			return withscores
					   ? MultiBulkCommand(RedisConstants.ZRevRange, key.ToBytes(),
											   start.ToBytes(), stop.ToBytes(), RedisConstants.WithScores)
					   : MultiBulkCommand(RedisConstants.ZRevRange, key.ToBytes(),
											   start.ToBytes(), stop.ToBytes());
		}

		public Task<MultiBulk> ZRevRangeByScoreAsync(string key, long min, long max, bool withScores = false)
		{
			return ZRevRangeByScoreAsync(key, min.ToBytes(), max.ToBytes(), null, null, withScores);
		}

		public Task<MultiBulk> ZRevRangeByScoreAsync(string key, long min, long max,
			long limitOffset, long limitCount, bool withScores = false)
		{
			return ZRevRangeByScoreAsync(key, min.ToBytes(), max.ToBytes(),
				limitOffset, limitCount, withScores);
		}

		public Task<MultiBulk> ZRevRangeByScoreAsync(string key, double min, double max, bool withScores = false)
		{
			return ZRevRangeByScoreAsync(key, min.ToBytes(), max.ToBytes(), null, null, withScores);
		}

		public Task<MultiBulk> ZRevRangeByScoreAsync(string key, double min, double max,
			long limitOffset, long limitCount, bool withScores = false)
		{
			return ZRevRangeByScoreAsync(key, min.ToBytes(), max.ToBytes(), limitOffset, limitCount, withScores);
		}

		private Task<MultiBulk> ZRevRangeByScoreAsync(string key, byte[] min, byte[] max,
			long? limitOffset, long? limitCount, bool withscores = false)
		{
			if (limitOffset.HasValue && withscores)
				return MultiBulkCommand(RedisConstants.ZRevRangeByScore, key.ToBytes(),
											 min, max, RedisConstants.WithScores,
											 RedisConstants.Limit, limitOffset.Value.ToBytes(),
											 limitCount.Value.ToBytes());
			if (limitOffset.HasValue)
				return MultiBulkCommand(RedisConstants.ZRevRangeByScore, key.ToBytes(),
											 min, max,
											 RedisConstants.Limit, limitOffset.Value.ToBytes(),
											 limitCount.Value.ToBytes());
			if (withscores)
				return MultiBulkCommand(RedisConstants.ZRevRangeByScore, key.ToBytes(),
											 min, max, RedisConstants.WithScores);

			return MultiBulkCommand(RedisConstants.ZRevRangeByScore, key.ToBytes(), min, max);
		}

		public Task<double> ZScoreAsync<T>(string key, T member)
		{
			return ZScoreAsync(key, Serialize(member));
		}

		public Task<double> ZScoreAsync(string key, byte[] member)
		{
			return BulkCommand(RedisConstants.ZScore, key.ToBytes(), member)
					.ContinueWithIfNoError(t => Deserialize<double>(t.Result));
		}

		public Task<long> ZUnionStoreAsync(string destinationKey, params string[] keys)
		{
			var request = ComposeRequest(RedisConstants.ZUnionStore, destinationKey.ToBytes(),
										 keys.Length.ToBytes(), keys);
			return IntegerCommand(request);
		}

		public Task<long> ZUnionStoreAsync(string destinationKey, Aggregation aggregation, params string[] keys)
		{
			var request = ComposeRequest(RedisConstants.ZUnionStore, destinationKey.ToBytes(),
										 keys.Length.ToBytes(), keys, RedisConstants.Aggregate,
										 aggregation.ToBytes());
			return IntegerCommand(request);
		}

		public Task<long> ZUnionStoreAsync(string destinationKey, params ZAggrStoreArgs[] keys)
		{
			var request = ComposeRequest(RedisConstants.ZUnionStore, destinationKey.ToBytes(),
										 keys.Length.ToBytes(), keys, RedisConstants.Weights);
			return IntegerCommand(request);
		}

		public Task<long> ZUnionStoreAsync(string destinationKey, Aggregation aggregation, params ZAggrStoreArgs[] keys)
		{
			var request = ComposeRequest(RedisConstants.ZUnionStore, destinationKey.ToBytes(),
										 keys.Length.ToBytes(), keys, RedisConstants.Weights,
										 RedisConstants.Aggregate, aggregation.ToBytes());
		
			return IntegerCommand(request);
		}

		#region request composing
		private byte[][] ComposeRequest(byte[] commandName, byte[] arg1, ZAddArgs[] args)
		{
			if (args.Length == 0)
				throw new ArgumentException("Invalid args count", "args");

			var request = new byte[2 + args.Length * 2][];
			request[0] = commandName;
			request[1] = arg1;

			for (int i = 0; i < args.Length; i++)
			{
				var arg = args[i];
				request[i * 2 + 2] = arg.UseIntValue
									   ? arg.IntScore.ToBytes()
									   : arg.DoubleScore.ToBytes();
				request[i * 2 + 3] = arg.IsArray ? (byte[])arg.Member : Serialize(arg.Member);
			}
			return request;
		}

		private byte[][] ComposeRequest(byte[] commandName, byte[] arg1, byte[] arg2, string[] args, byte[] lastArg1,
										byte[] lastArg2)
		{
			if (args.Length == 0)
				throw new ArgumentException("Invalid args count", "args");

			var request = new byte[5 + args.Length][];
			request[0] = commandName;
			request[1] = arg1;
			request[2] = arg2;

			for (int i = 0; i < args.Length; i++)
				request[i + 3] = args[i].ToBytes();

			request[3 + args.Length] = lastArg1;
			request[3 + args.Length + 1] = lastArg2;
			return request;
		}
		private byte[][] ComposeRequest(byte[] commandName, byte[] arg1, byte[] arg2, ZAggrStoreArgs[] args,
										byte[] lastArg1,byte[] lastArg2 = null,byte[] lastArg3 =null)
		{
			if (args.Length == 0)
				throw new ArgumentException("Invalid args count", "args");

			var request = new byte[((lastArg2 != null && lastArg3!=null)?6:4) + args.Length * 2][];
			request[0] = commandName;
			request[1] = arg1;
			request[2] = arg2;

			for (int i = 0; i < args.Length; i++)
				request[i + 3] = args[i].Key.ToBytes();

			request[3 + args.Length] = lastArg1;

			for (int i = 0; i < args.Length; i++)
				request[i + 4 + args.Length] = args[i].Weight.ToBytes();

			if (lastArg2 != null && lastArg3 != null)
			{
				request[4 + args.Length*2] = lastArg2;
				request[5 + args.Length*2] = lastArg3;
			}

			return request;
		}
		#endregion
	}
}
