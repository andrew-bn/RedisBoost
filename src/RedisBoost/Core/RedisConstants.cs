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
using System.Text;

namespace RedisBoost.Core
{
	public static class RedisConstants
	{
		public static byte[] SerializeCommandName(string command)
		{
			return GetBytes(command.ToUpper());
		}

		private static byte[] GetBytes(string value)
		{
			return Encoding.UTF8.GetBytes(value);
		}

		//common
		public const int DefaultPort = 6379;
		public static DateTime InitialUnixTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
		public static byte Minus = GetBytes("-")[0];
		public static byte Colon = GetBytes(":")[0];
		public static byte Plus = GetBytes("+")[0];
		public static byte Asterix = GetBytes("*")[0];
		public static byte Dollar = GetBytes("$")[0];
		public static byte[] NewLine = GetBytes("\r\n");
		public static byte[] Before = GetBytes("BEFORE");
		public static byte[] After = GetBytes("AFTER");
		public static byte[] NegativeInfinity = GetBytes("-inf");
		public static byte[] PositiveInfinity = GetBytes("+inf");
		public static byte[] Weights = GetBytes("WEIGHTS");
		public static byte[] Sum = GetBytes("SUM");
		public static byte[] Min = GetBytes("MIN");
		public static byte[] Max = GetBytes("MAX");
		public static byte[] Aggregate = GetBytes("AGGREGATE");
		public static byte[] WithScores = GetBytes("WITHSCORES");
		public static byte[] Limit = GetBytes("LIMIT");
		public static byte[] RefCount = GetBytes("REFCOUNT");
		public static byte[] ObjEncoding = GetBytes("ENCODING");
		public static byte[] IdleTime = GetBytes("IDLETIME");
		public static byte[] By = GetBytes("BY");
		public static byte[] Asc = GetBytes("ASC");
		public static byte[] Desc = GetBytes("DESC");
		public static byte[] Alpha = GetBytes("ALPHA");
		public static byte[] Store = GetBytes("STORE");
		public static byte[] And = GetBytes("AND");
		public static byte[] Or = GetBytes("OR");
		public static byte[] Xor = GetBytes("XOR");
		public static byte[] Not = GetBytes("NOT");
		public static byte[] Channels = GetBytes("CHANNELS");
		public static byte[] NumSub = GetBytes("NUMSUB");
		public static byte[] NumPat = GetBytes("NUMPAT");
		//keys
		public readonly static byte[] Del = GetBytes("DEL");
		public readonly static byte[] Dump = GetBytes("DUMP");
		public readonly static byte[] Move = GetBytes("MOVE");
		public readonly static byte[] Object = GetBytes("OBJECT");
		public readonly static byte[] Restore = GetBytes("RESTORE");
		public readonly static byte[] Migrate = GetBytes("MIGRATE");
		public readonly static byte[] Sort = GetBytes("SORT");
		public readonly static byte[] Exists = GetBytes("EXISTS");
		public readonly static byte[] Expire = GetBytes("EXPIRE");
		public readonly static byte[] PExpire = GetBytes("PEXPIRE");
		public readonly static byte[] ExpireAt = GetBytes("EXPIREAT");
		public readonly static byte[] Persist = GetBytes("PERSIST");
		public readonly static byte[] Keys = GetBytes("KEYS");
		public readonly static byte[] Pttl = GetBytes("PTTL");
		public readonly static byte[] Ttl = GetBytes("TTL");
		public readonly static byte[] RandomKey = GetBytes("RANDOMKEY");
		public readonly static byte[] Rename = GetBytes("RENAME");
		public readonly static byte[] RenameNx = GetBytes("RENAMENX");
		public readonly static byte[] Type = GetBytes("TYPE");

		//strings
		public readonly static byte[] Decr = GetBytes("DECR");
		public readonly static byte[] Incr = GetBytes("INCR");
		public readonly static byte[] BitOp = GetBytes("BITOP");
		public readonly static byte[] GetBit = GetBytes("GETBIT");
		public readonly static byte[] SetBit = GetBytes("SETBIT");
		public readonly static byte[] DecrBy = GetBytes("DECRBY");
		public readonly static byte[] IncrBy = GetBytes("INCRBY");
		public readonly static byte[] IncrByFloat = GetBytes("INCRBYFLOAT");
		public readonly static byte[] GetRange = GetBytes("GETRANGE");
		public readonly static byte[] SetRange = GetBytes("SETRANGE");
		public readonly static byte[] GetSet = GetBytes("GETSET");
		public readonly static byte[] MGet = GetBytes("MGET");
		public readonly static byte[] MSet = GetBytes("MSET");
		public readonly static byte[] MSetNx = GetBytes("MSETNX");
		public readonly static byte[] Append = GetBytes("APPEND");
		public readonly static byte[] BitCount = GetBytes("BITCOUNT");
		public readonly static byte[] Get = GetBytes("GET");
		public readonly static byte[] Set = GetBytes("SET");
		public readonly static byte[] PSetEx = GetBytes("PSETEX");
		public readonly static byte[] SetEx = GetBytes("SETEX");
		public readonly static byte[] SetNx = GetBytes("SETNX");
		public readonly static byte[] StrLen = GetBytes("STRLEN");

		//hashes
		public readonly static byte[] HSet = GetBytes("HSET");
		public readonly static byte[] HSetNx = GetBytes("HSETNX");
		public readonly static byte[] HExists = GetBytes("HEXISTS");
		public readonly static byte[] HDel = GetBytes("HDEL");
		public readonly static byte[] HGet = GetBytes("HGET");
		public readonly static byte[] HGetAll = GetBytes("HGETALL");
		public readonly static byte[] HIncrBy = GetBytes("HINCRBY");
		public readonly static byte[] HIncrByFloat = GetBytes("HINCRBYFLOAT");
		public readonly static byte[] HKeys = GetBytes("HKEYS");
		public readonly static byte[] HVals = GetBytes("HVALS");
		public readonly static byte[] HLen = GetBytes("HLEN");
		public readonly static byte[] HMGet = GetBytes("HMGET");
		public readonly static byte[] HMSet = GetBytes("HMSET");

		//lists
		public readonly static byte[] BlPop = GetBytes("BLPOP");
		public readonly static byte[] LPush = GetBytes("LPUSH");
		public readonly static byte[] LPushX = GetBytes("LPUSHX");
		public readonly static byte[] BrPop = GetBytes("BRPOP");
		public readonly static byte[] RPush = GetBytes("RPUSH");
		public readonly static byte[] RPushX = GetBytes("RPUSHX");
		public readonly static byte[] LPop = GetBytes("LPOP");
		public readonly static byte[] RPop = GetBytes("RPOP");
		public readonly static byte[] RPopLPush = GetBytes("RPOPLPUSH");
		public readonly static byte[] BRPopLPush = GetBytes("BRPOPLPUSH");
		public readonly static byte[] LIndex = GetBytes("LINDEX");
		public readonly static byte[] LInsert = GetBytes("LINSERT");
		public readonly static byte[] LLen = GetBytes("LLEN");
		public readonly static byte[] LRange = GetBytes("LRANGE");
		public readonly static byte[] LRem = GetBytes("LREM");
		public readonly static byte[] LSet = GetBytes("LSET");
		public readonly static byte[] LTrim = GetBytes("LTRIM");
		
		//sets
		public readonly static byte[] SAdd = GetBytes("SADD");
		public readonly static byte[] SCard = GetBytes("SCARD");
		public readonly static byte[] SDiff = GetBytes("SDIFF");
		public readonly static byte[] SInter = GetBytes("SINTER");
		public readonly static byte[] SUnion = GetBytes("SUNION");
		public readonly static byte[] SDiffStore = GetBytes("SDIFFSTORE");
		public readonly static byte[] SInterStore = GetBytes("SINTERSTORE");
		public readonly static byte[] SUnionStore = GetBytes("SUNIONSTORE");
		public readonly static byte[] SIsMember = GetBytes("SISMEMBER");
		public readonly static byte[] SMembers = GetBytes("SMEMBERS");
		public readonly static byte[] SMove = GetBytes("SMOVE");
		public readonly static byte[] SPop = GetBytes("SPOP");
		public readonly static byte[] SRandMember = GetBytes("SRANDMEMBER");
		public readonly static byte[] SRem = GetBytes("SREM");
		
		//sorted sets
		public readonly static byte[] ZAdd = GetBytes("ZADD");
		public readonly static byte[] ZRem = GetBytes("ZREM");
		public readonly static byte[] ZCard = GetBytes("ZCARD");
		public readonly static byte[] ZCount = GetBytes("ZCOUNT");
		public readonly static byte[] ZIncrBy = GetBytes("ZINCRBY");
		public readonly static byte[] ZInterStore = GetBytes("ZINTERSTORE");
		public readonly static byte[] ZUnionStore = GetBytes("ZUNIONSTORE");
		public readonly static byte[] ZRange = GetBytes("ZRANGE");
		public readonly static byte[] ZRangeByScore = GetBytes("ZRANGEBYSCORE");
		public readonly static byte[] ZRevRangeByScore = GetBytes("ZREVRANGEBYSCORE");
		public readonly static byte[] ZRank = GetBytes("ZRANK");
		public readonly static byte[] ZRemRangeByRank = GetBytes("ZREMRANGEBYRANK");
		public readonly static byte[] ZRemRangeByScore = GetBytes("ZREMRANGEBYSCORE");
		public readonly static byte[] ZRevRange = GetBytes("ZREVRANGE");
		public readonly static byte[] ZRevRank = GetBytes("ZREVRANK");
		public readonly static byte[] ZScore = GetBytes("ZSCORE");

		//pub/sub
		public readonly static byte[] Subscribe = GetBytes("SUBSCRIBE");
		public readonly static byte[] Unsubscribe = GetBytes("UNSUBSCRIBE");
		public readonly static byte[] Publish = GetBytes("PUBLISH");
		public readonly static byte[] PSubscribe = GetBytes("PSUBSCRIBE");
		public readonly static byte[] PUnsubscribe = GetBytes("PUNSUBSCRIBE");
		public readonly static byte[] PubSub = GetBytes("PUBSUB");
		
		//transactions
		public readonly static byte[] Discard = GetBytes("DISCARD");
		public readonly static byte[] Exec = GetBytes("EXEC");
		public readonly static byte[] Multi = GetBytes("MULTI");
		public readonly static byte[] Unwatch = GetBytes("UNWATCH");
		public readonly static byte[] Watch = GetBytes("WATCH");

		// hyperloglog
		public readonly static byte[] PfAdd = GetBytes("PFADD");
		public readonly static byte[] PfCount = GetBytes("PFCOUNT");
		public readonly static byte[] PfMerge = GetBytes("PFMERGE");

		//scripting
		public readonly static byte[] Eval = GetBytes("EVAL");
		public readonly static byte[] Script = GetBytes("SCRIPT");
		public readonly static byte[] Load = GetBytes("LOAD");
		public readonly static byte[] EvalSha = GetBytes("EVALSHA");
		public readonly static byte[] Flush = GetBytes("FLUSH");
		public readonly static byte[] Kill = GetBytes("Kill");

		//connection
		public readonly static byte[] Auth = GetBytes("AUTH");
		public readonly static byte[] Echo = GetBytes("ECHO");
		public readonly static byte[] Ping = GetBytes("PING");
		public readonly static byte[] Quit = GetBytes("QUIT");

		//server
		public readonly static byte[] FlushDb = GetBytes("FLUSHDB");
		public readonly static byte[] FlushAll = GetBytes("FLUSHALL");
		public readonly static byte[] Select = GetBytes("SELECT");
		public readonly static byte[] BgRewriteAof = GetBytes("BGREWRITEAOF");
		public readonly static byte[] BgSave = GetBytes("BGSAVE");
		public readonly static byte[] Client = GetBytes("CLIENT");
		public readonly static byte[] List = GetBytes("LIST");
		public readonly static byte[] GetName = GetBytes("GETNAME");
		public readonly static byte[] SetName = GetBytes("SETNAME");
		public readonly static byte[] DbSize = GetBytes("DBSIZE");
		public readonly static byte[] Config = GetBytes("CONFIG");
		public readonly static byte[] ResetStat = GetBytes("RESETSTAT");
		public readonly static byte[] Info = GetBytes("INFO");
		public readonly static byte[] Save = GetBytes("SAVE");
		public readonly static byte[] ShutDown = GetBytes("SHUTDOWN");
		public readonly static byte[] LastSave = GetBytes("LASTSAVE");
		public readonly static byte[] NoSave = GetBytes("NOSAVE");
		public readonly static byte[] SlaveOf = GetBytes("SLAVEOF");
		public readonly static byte[] Time = GetBytes("TIME");
		public readonly static byte[] No = GetBytes("NO");
		public readonly static byte[] One = GetBytes("ONE");

		//cluster
		public readonly static byte[] Readonly = GetBytes("READONLY");
		public readonly static byte[] ReadWrite = GetBytes("READWRITE");
	}
}
