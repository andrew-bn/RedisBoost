using System;
using System.Threading.Tasks;

namespace NBoosters.RedisBoost
{
	public interface IRedisClient : IDisposable
	{
		string ConnectionString { get; }

		Task DisconnectAsync();

		Task<string> ClientKillAsync(string ip, int port);
		Task<byte[][]> KeysAsync(string pattern);
		Task<long> DelAsync(string key);
		Task<byte[]> DumpAsync(string key);
		Task<long> MoveAsync(string key, int db);
		Task<RedisResponse> ObjectAsync(Subcommand subcommand, params string[] args);
		Task<RedisResponse> SortAsync(string key, string by = null, long? limitOffset = null,
		                         long? limitCount = null, bool? asc = null, bool alpha = false, string destination = null,
		                         string[] getPatterns = null);
		Task<string> RestoreAsync(string key, int ttlInMilliseconds, byte[] serializedValue);
		Task<long> ExistsAsync(string key);
		Task<long> ExpireAsync(string key, int seconds);
		Task<long> PExpireAsync(string key, int milliseconds);
		Task<long> ExpireAtAsync(string key, DateTime timestamp);
		Task<long> PersistAsync(string key);
		Task<long> PttlAsync(string key);
		Task<long> TtlAsync(string key);
		Task<string> TypeAsync(string key);
		Task<string> RandomKeyAsync();
		Task<string> RenameAsync(string key, string newKey);
		Task<long> RenameNxAsync(string key, string newKey);
		Task<string> FlushDbAsync();
		Task<string> FlushAllAsync();
		Task<string> BgRewriteAofAsync();
		Task<string> BgSaveAsync();
		Task<byte[]> ClientListAsync();
		Task<long> DbSizeAsync();
		Task<RedisResponse> ConfigGetAsync(string parameter);
		Task<string> ConfigSetAsync(string parameter, byte[] value);
		Task<string> ConfigResetStatAsync();
		Task<byte[]> InfoAsync();
		Task<byte[]> InfoAsync(string section);
		Task<long> LastSaveAsync();
		Task<string> SaveAsync();
		Task<string> ShutDownAsync();
		Task<string> ShutDownAsync(bool save);
		Task<string> SlaveOfAsync(string host, int port);
		Task<byte[][]> TimeAsync();
		Task<string> AuthAsync(string password);
		Task<byte[]> EchoAsync(byte[] message);
		Task<string> PingAsync();
		Task<string> QuitAsync();
		Task<string> SelectAsync(int index);
		Task<long> HSetAsync(string key, string field, byte[] value);
		Task<long> HSetAsync(string key, byte[] field, byte[] value);
		Task<long> HSetNxAsync(string key, string field, byte[] value);
		Task<long> HSetNxAsync(string key, byte[] field, byte[] value);
		Task<long> HExistsAsync(string key, string field);
		Task<long> HExistsAsync(string key, byte[] field);
		Task<long> HDelAsync(string key, params string[] fields);
		Task<long> HDelAsync(string key, params byte[][] fields);
		Task<byte[]> HGetAsync(string key, string field);
		Task<byte[]> HGetAsync(string key, byte[] field);
		Task<byte[][]> HGetAllAsync(string key);
		Task<long> HIncrByAsync(string key, string field, int increment);
		Task<long> HIncrByAsync(string key, byte[] field, int increment);
		Task<byte[][]> HKeysAsync(string key);
		Task<byte[][]> HValsAsync(string key);
		Task<long> HLenAsync(string key);
		Task<byte[][]> HMGetAsync(string key, params string[] fields);
		Task<byte[][]> HMGetAsync(string key, params byte[][] fields);
		Task<string> HMSetAsync(string key, params MSetArgs[] args);
		Task<RedisResponse> EvalAsync(string script, string[] keys, params byte[][] arguments);
		Task<RedisResponse> EvalShaAsync(byte[] sha1, string[] keys, params byte[][] arguments);
		Task<byte[]> ScriptLoadAsync(string script);
		Task<byte[][]> ScriptExistsAsync(params byte[][] sha1);
		Task<string> ScriptFlushAsync();
		Task<string> ScriptKillAsync();
		Task<long> ZAddAsync(string key, long score, byte[] member);
		Task<long> ZAddAsync(string key, double score, byte[] member);
		Task<long> ZAddAsync(string key, params ZAddArgs[] args);
		Task<long> ZRemAsync(string key, params byte[][] members);
		Task<long> ZRemRangeByRankAsync(string key, long start, long stop);
		Task<long> ZRemRangeByScoreAsync(string key, long min, long max);
		Task<long> ZRemRangeByScoreAsync(string key, double min, double max);
		Task<long> ZCardAsync(string key);
		Task<long> ZCountAsync(string key, long min, long max);
		Task<long> ZCountAsync(string key, double min, double max);
		Task<byte[]> ZIncrByAsync(string key, long min, byte[] value);
		Task<byte[]> ZIncrByAsync(string key, double min, byte[] value);
		Task<long> ZInterStoreAsync(string destinationKey, params string[] keys);
		Task<long> ZInterStoreAsync(string destinationKey, Aggregation aggregation, params string[] keys);
		Task<long> ZInterStoreAsync(string destinationKey, params ZAggrStoreArgs[] keys);
		Task<long> ZInterStoreAsync(string destinationKey, Aggregation aggregation, params ZAggrStoreArgs[] keys);
		Task<byte[][]> ZRangeAsync(string key, long start, long stop, bool withScores = false);
		Task<byte[][]> ZRangeByScoreAsync(string key, long min, long max, bool withScores = false);

		Task<byte[][]> ZRangeByScoreAsync(string key, long min, long max,
														  long limitOffset, long limitCount, bool withScores = false);

		Task<byte[][]> ZRangeByScoreAsync(string key, double min, double max, bool withScores = false);

		Task<byte[][]> ZRangeByScoreAsync(string key, double min, double max,
														  long limitOffset, long limitCount, bool withScores = false);

		Task<long?> ZRankAsync(string key, byte[] member);
		Task<long?> ZRevRankAsync(string key, byte[] member);
		Task<byte[][]> ZRevRangeAsync(string key, long start, long stop, bool withscores = false);
		Task<byte[][]> ZRevRangeByScoreAsync(string key, long min, long max, bool withScores = false);

		Task<byte[][]> ZRevRangeByScoreAsync(string key, long min, long max,
															 long limitOffset, long limitCount, bool withScores = false);

		Task<byte[][]> ZRevRangeByScoreAsync(string key, double min, double max, bool withScores = false);

		Task<byte[][]> ZRevRangeByScoreAsync(string key, double min, double max,
															 long limitOffset, long limitCount, bool withScores = false);

		Task<byte[]> ZScoreAsync(string key, byte[] member);
		Task<long> ZUnionStoreAsync(string destinationKey, params string[] keys);
		Task<long> ZUnionStoreAsync(string destinationKey, Aggregation aggregation, params string[] keys);
		Task<long> ZUnionStoreAsync(string destinationKey, params ZAggrStoreArgs[] keys);
		Task<long> ZUnionStoreAsync(string destinationKey, Aggregation aggregation, params ZAggrStoreArgs[] keys);
		Task<byte[]> GetAsync(string key);
		Task<string> SetAsync(string key, byte[] value);
		Task<long> AppendAsync(string key, byte[] value);
		Task<long> BitCountAsync(string key);
		Task<long> BitCountAsync(string key, int start, int end);
		Task<long> DecrAsync(string key);
		Task<long> IncrAsync(string key);
		Task<long> DecrByAsync(string key, int decrement);
		Task<long> IncrByAsync(string key, int increment);
		Task<byte[]> GetRangeAsync(string key, int start, int end);
		Task<long> SetRangeAsync(string key, int offset, byte[] value);
		Task<long> StrLenAsync(string key);
		Task<byte[]> GetSetAsync(string key, byte[] value);
		Task<byte[][]> MGetAsync(params string[] key);
		Task<string> MSetAsync(params MSetArgs[] args);
		Task<long> MSetNxAsync(params MSetArgs[] args);
		Task<string> SetExAsync(string key, int seconds, byte[] value);
		Task<string> PSetExAsync(string key, int milliseconds, byte[] value);
		Task<long> SetNxAsync(string key, byte[] value);
		Task<long> SAddAsync(string key, params byte[][] member);
		Task<long> SRemAsync(string key, params byte[][] member);
		Task<long> SCardAsync(string key);
		Task<byte[][]> SDiffAsync(params string[] keys);
		Task<long> SDiffStoreAsync(string destinationKey, params string[] keys);
		Task<byte[][]> SUnionAsync(params string[] keys);
		Task<long> SUnionStoreAsync(string destinationKey, params string[] keys);
		Task<byte[][]> SInterAsync(params string[] keys);
		Task<long> SInterStoreAsync(string destinationKey, params string[] keys);
		Task<long> SIsMemberAsync(string key, byte[] value);
		Task<byte[][]> SMembersAsync(string key);
		Task<long> SMoveAsync(string sourceKey, string destinationKey, byte[] member);
		Task<byte[]> SPopAsync(string key);
		Task<byte[]> SRandMemberAsync(string key);
		Task<byte[][]> SRandMemberAsync(string key, int count);
		Task<byte[][]> BlPopAsync(int timeoutInSeconds, params string[] keys);
		Task<long> LPushAsync(string key, params byte[][] values);
		Task<byte[][]> BrPopAsync(int timeoutInSeconds, params string[] keys);
		Task<long> RPushAsync(string key, params byte[][] values);
		Task<byte[]> LPopAsync(string key);
		Task<byte[]> RPopAsync(string key);
		Task<byte[]> RPopLPushAsync(string source, string destination);
		Task<byte[]> BRPopLPushAsync(string sourceKey, string destinationKey, int timeoutInSeconds);
		Task<byte[]> LIndexAsync(string key, int index);
		Task<long> LInsertAsync(string key, byte[] pivot, byte[] value, bool before = true);
		Task<long> LLenAsync(string key);
		Task<long> LPushXAsync(string key, byte[] values);
		Task<byte[][]> LRangeAsync(string key, int start, int stop);
		Task<long> LRemAsync(string key, int count, byte[] value);
		Task<string> LSetAsync(string key, int index, byte[] value);
		Task<string> LTrimAsync(string key, int start, int stop);
		Task<long> RPushXAsync(string key, byte[] values);
		Task<string> DiscardAsync();
		Task<byte[][]> ExecAsync();
		Task<string> MultiAsync();
		Task<string> UnwatchAsync();
		Task<string> WatchAsync(params string[] keys);
		Task<long> PublishAsync(string channel, byte[] message);

		Task<IRedisSubscription> SubscribeAsync(params string[] channels);
		Task<IRedisSubscription> PSubscribeAsync(params string[] pattern);

	}
}
