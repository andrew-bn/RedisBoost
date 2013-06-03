#region Apache Licence, Version 2.0
/*
 Copyright 2013 Andrey Bulygin.

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
using NBoosters.RedisBoost.Core.Serialization;

namespace NBoosters.RedisBoost
{
	public interface IRedisClient : IDisposable
	{
		IRedisSerializer Serializer { get; }
		string ConnectionString { get; }

		Task DisconnectAsync();
		/// <summary>
		/// Kill the connection of a client. 
		/// Complexity: O(N) where N is the number of client connections
		/// </summary>
		/// <param name="ip"></param>
		/// <param name="port"></param>
		/// <returns></returns>
		Task<string> ClientKillAsync(string ip, int port);
		/// <summary>
		/// Find all keys matching the given pattern.
		/// Complexity: O(N) with N being the number of keys in the database, under the assumption that the key names in the database and the given pattern have limited length.
		/// </summary>
		/// <param name="pattern"></param>
		/// <returns></returns>
		Task<MultiBulk> KeysAsync(string pattern);
		/// <summary>
		/// Delete a key.
		/// Complexity: O(N) where N is the number of keys that will be removed. When a key to remove holds a value other than a string, the individual complexity for this key is O(M) where M is the number of elements in the list, set, sorted set or hash. Removing a single key that holds a string value is O(1).
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		Task<long> DelAsync(string key);
		/// <summary>
		/// Perform bitwise operations between strings.
		/// Complexity: O(N)
		/// </summary>
		/// <param name="bitOp"></param>
		/// <param name="destKey"></param>
		/// <param name="keys"></param>
		/// <returns></returns>
		Task<long> BitOpAsync(BitOpType bitOp, string destKey, params string[] keys);
		/// <summary>
		/// Sets or clears the bit at offset in the string value stored at key. Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="offset"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		Task<long> SetBitAsync(string key, long offset, int value);
		/// <summary>
		/// Returns the bit value at offset in the string value stored at key. Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="offset"></param>
		/// <returns></returns>
		Task<long> GetBitAsync(string key, long offset);
		/// <summary>
		/// Atomically transfer a key from a Redis instance to another one. 
		/// Complexity: This command actually executes a DUMP+DEL in the source instance, and a RESTORE in the target instance. See the pages of these commands for time complexity. Also an O(N) data transfer between the two instances is performed.
		/// </summary>
		/// <param name="host"></param>
		/// <param name="port"></param>
		/// <param name="key"></param>
		/// <param name="destinationDb"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		Task<string> MigrateAsync(string host, int port, string key, int destinationDb, int timeout);
		/// <summary>
		/// Return a serialized version of the value stored at the specified key.
		/// Complexity: O(1) to access the key and additional O(N*M) to serialized it, where N is the number of Redis objects composing the value and M their average size. For small string values the time complexity is thus O(1)+O(1*M) where M is small, so simply O(1).
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		Task<Bulk> DumpAsync(string key);
		/// <summary>
		/// Move a key to another database. Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="db"></param>
		/// <returns></returns>
		Task<long> MoveAsync(string key, int db);
		/// <summary>
		/// Inspect the internals of Redis objects. Complexity: O(1) for all the currently implemented subcommands.
		/// </summary>
		/// <param name="subcommand"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		Task<RedisResponse> ObjectAsync(Subcommand subcommand, params string[] args);
		/// <summary>
		/// Sort the elements in a list, set or sorted set.
		/// Complexity: O(N+M*log(M)) where N is the number of elements in the list or set to sort, and M the number of returned elements. When the elements are not sorted, complexity is currently O(N) as there is a copy step that will be avoided in next releases.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="by"></param>
		/// <param name="limitOffset"></param>
		/// <param name="limitCount"></param>
		/// <param name="asc"></param>
		/// <param name="alpha"></param>
		/// <param name="destination"></param>
		/// <param name="getPatterns"></param>
		/// <returns></returns>
		Task<RedisResponse> SortAsync(string key, string by = null, long? limitOffset = null,
		                         long? limitCount = null, bool? asc = null, bool alpha = false, string destination = null,
		                         string[] getPatterns = null);
		/// <summary>
		/// Create a key using the provided serialized value, previously obtained using DUMP.
		/// Complexity: O(1) to create the new key and additional O(N*M) to recostruct the serialized value, where N is the number of Redis objects composing the value and M their average size. For small string values the time complexity is thus O(1)+O(1*M) where M is small, so simply O(1). However for sorted set values the complexity is O(N*M*log(N)) because inserting values into sorted sets is O(log(N)).
		/// </summary>
		/// <param name="key"></param>
		/// <param name="ttlInMilliseconds"></param>
		/// <param name="serializedValue"></param>
		/// <returns></returns>
		Task<string> RestoreAsync(string key, int ttlInMilliseconds, byte[] serializedValue);
		/// <summary>
		/// Determine if a key exists.
		/// Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		Task<long> ExistsAsync(string key);
		/// <summary>
		/// Set a key's time to live in seconds.
		/// Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="seconds"></param>
		/// <returns></returns>
		Task<long> ExpireAsync(string key, int seconds);
		/// <summary>
		/// Set a key's time to live in milliseconds.
		/// Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="milliseconds"></param>
		/// <returns></returns>
		Task<long> PExpireAsync(string key, int milliseconds);
		/// <summary>
		/// Set a key's time to live in seconds.
		/// Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="timestamp"></param>
		/// <returns></returns>
		Task<long> ExpireAtAsync(string key, DateTime timestamp);
		/// <summary>
		/// Remove the expiration from a key. Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		Task<long> PersistAsync(string key);
		/// <summary>
		/// Get the time to live for a key in milliseconds. Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		Task<long> PttlAsync(string key);
		/// <summary>
		/// Get the time to live for a key. Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		Task<long> TtlAsync(string key);
		/// <summary>
		/// Returns the string representation of the type of the value stored at key. The different types that can be returned are: string, list, set, zset and hash.
		/// Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		Task<string> TypeAsync(string key);
		/// <summary>
		/// Return a random key from the keyspace. Complexity: O(1)
		/// </summary>
		/// <returns></returns>
		Task<string> RandomKeyAsync();
		/// <summary>
		/// Rename a key. Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="newKey"></param>
		/// <returns></returns>
		Task<string> RenameAsync(string key, string newKey);
		/// <summary>
		/// Rename a key, only if the new key does not exist.
		/// Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="newKey"></param>
		/// <returns></returns>
		Task<long> RenameNxAsync(string key, string newKey);
		/// <summary>
		/// Remove all keys from the current database
		/// </summary>
		/// <returns></returns>
		Task<string> FlushDbAsync();
		/// <summary>
		/// Remove all keys from all databases
		/// </summary>
		/// <returns></returns>
		Task<string> FlushAllAsync();
		/// <summary>
		/// Asynchronously rewrite the append-only file
		/// </summary>
		/// <returns></returns>
		Task<string> BgRewriteAofAsync();
		/// <summary>
		/// Asynchronously save the dataset to disk
		/// </summary>
		/// <returns></returns>
		Task<string> BgSaveAsync();
		/// <summary>
		/// Get the list of client connections. Complexity: O(N) where N is the number of client connections.
		/// </summary>
		/// <returns></returns>
		Task<Bulk> ClientListAsync();
		/// <summary>
		/// Return the number of keys in the selected database
		/// </summary>
		/// <returns></returns>
		Task<long> DbSizeAsync();
		/// <summary>
		/// Get the value of a configuration parameter
		/// </summary>
		/// <param name="parameter"></param>
		/// <returns></returns>
		Task<RedisResponse> ConfigGetAsync(string parameter);
		/// <summary>
		/// Set a configuration parameter to the given value
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="parameter"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		Task<string> ConfigSetAsync<T>(string parameter, T value);
		/// <summary>
		/// Set a configuration parameter to the given value
		/// </summary>
		/// <param name="parameter"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		Task<string> ConfigSetAsync(string parameter, byte[] value);
		/// <summary>
		/// Reset the stats returned by INFO. Complexity: O(1)
		/// </summary>
		/// <returns></returns>
		Task<string> ConfigResetStatAsync();
		/// <summary>
		/// Get information and statistics about the server
		/// </summary>
		/// <returns></returns>
		Task<Bulk> InfoAsync();
		/// <summary>
		/// Get information and statistics about the server
		/// </summary>
		/// <param name="section"></param>
		/// <returns></returns>
		Task<Bulk> InfoAsync(string section);
		/// <summary>
		/// Get the UNIX time stamp of the last successful save to disk
		/// </summary>
		/// <returns></returns>
		Task<long> LastSaveAsync();
		/// <summary>
		/// Synchronously save the dataset to disk
		/// </summary>
		/// <returns></returns>
		Task<string> SaveAsync();
		/// <summary>
		/// Synchronously save the dataset to disk and then shut down the server
		/// </summary>
		/// <returns></returns>
		Task<string> ShutDownAsync();
		/// <summary>
		/// Synchronously save the dataset to disk and then shut down the server
		/// </summary>
		/// <param name="save"></param>
		/// <returns></returns>
		Task<string> ShutDownAsync(bool save);
		/// <summary>
		/// Make the server a slave of another instance, or promote it as master
		/// </summary>
		/// <param name="host"></param>
		/// <param name="port"></param>
		/// <returns></returns>
		Task<string> SlaveOfAsync(string host, int port);
		/// <summary>
		/// Return the current server time. Complexity: O(1)
		/// </summary>
		/// <returns></returns>
		Task<MultiBulk> TimeAsync();
		/// <summary>
		/// Authenticate to the server
		/// </summary>
		/// <param name="password"></param>
		/// <returns></returns>
		Task<string> AuthAsync(string password);
		/// <summary>
		/// Echo the given string
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="message"></param>
		/// <returns></returns>
		Task<Bulk> EchoAsync<T>(T message);
		/// <summary>
		/// Echo the given string
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		Task<Bulk> EchoAsync(byte[] message);
		/// <summary>
		/// Ping the server
		/// </summary>
		/// <returns></returns>
		Task<string> PingAsync();
		/// <summary>
		/// Close the connection
		/// </summary>
		/// <returns></returns>
		Task<string> QuitAsync();
		/// <summary>
		/// Change the selected database for the current connection
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		Task<string> SelectAsync(int index);
		/// <summary>
		/// Set the string value of a hash field. Complexity: O(1)
		/// </summary>
		/// <typeparam name="TFld"></typeparam>
		/// <typeparam name="TVal"></typeparam>
		/// <param name="key"></param>
		/// <param name="field"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		Task<long> HSetAsync<TFld,TVal>(string key, TFld field, TVal value);
		/// <summary>
		/// Set the string value of a hash field. Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="field"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		Task<long> HSetAsync(string key, byte[] field, byte[] value);
		/// <summary>
		/// Set the value of a hash field, only if the field does not exist. Complexity: O(1)
		/// </summary>
		/// <typeparam name="TFld"></typeparam>
		/// <typeparam name="TVal"></typeparam>
		/// <param name="key"></param>
		/// <param name="field"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		Task<long> HSetNxAsync<TFld,TVal>(string key, TFld field, TVal value);
		/// <summary>
		/// Set the value of a hash field, only if the field does not exist. Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="field"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		Task<long> HSetNxAsync(string key, byte[] field, byte[] value);
		/// <summary>
		/// Determine if a hash field exists. Complexity: O(1)
		/// </summary>
		/// <typeparam name="TFld"></typeparam>
		/// <param name="key"></param>
		/// <param name="field"></param>
		/// <returns></returns>
		Task<long> HExistsAsync<TFld>(string key, TFld field);
		/// <summary>
		/// Determine if a hash field exists. Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="field"></param>
		/// <returns></returns>
		Task<long> HExistsAsync(string key, byte[] field);
		/// <summary>
		/// Delete one or more hash fields. Complexity: O(N) where N is the number of fields to be removed.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="fields"></param>
		/// <returns></returns>
		Task<long> HDelAsync(string key, params object[] fields);
		/// <summary>
		/// Delete one or more hash fields. Complexity: O(N) where N is the number of fields to be removed.
		/// </summary>
		/// <typeparam name="TFld"></typeparam>
		/// <param name="key"></param>
		/// <param name="fields"></param>
		/// <returns></returns>
		Task<long> HDelAsync<TFld>(string key, params TFld[] fields);
		/// <summary>
		/// Delete one or more hash fields. Complexity: O(N) where N is the number of fields to be removed.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="fields"></param>
		/// <returns></returns>
		Task<long> HDelAsync(string key, params byte[][] fields);
		/// <summary>
		/// Get the value of a hash field. Complexity: O(1)
		/// </summary>
		/// <typeparam name="TFld"></typeparam>
		/// <param name="key"></param>
		/// <param name="field"></param>
		/// <returns></returns>
		Task<Bulk> HGetAsync<TFld>(string key, TFld field);
		/// <summary>
		/// Get the value of a hash field. Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="field"></param>
		/// <returns></returns>
		Task<Bulk> HGetAsync(string key, byte[] field);
		/// <summary>
		/// Get all the fields and values in a hash. Complexity: O(N) where N is the size of the hash.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		Task<MultiBulk> HGetAllAsync(string key);
		/// <summary>
		/// Increment the integer value of a hash field by the given number. Complexity: O(1)
		/// </summary>
		/// <typeparam name="TFld"></typeparam>
		/// <param name="key"></param>
		/// <param name="field"></param>
		/// <param name="increment"></param>
		/// <returns></returns>
		Task<long> HIncrByAsync<TFld>(string key, TFld field, int increment);
		/// <summary>
		/// Increment the integer value of a hash field by the given number. Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="field"></param>
		/// <param name="increment"></param>
		/// <returns></returns>
		Task<long> HIncrByAsync(string key, byte[] field, int increment);
		/// <summary>
		/// Increment the float value of a hash field by the given amount. Complexity: O(1)
		/// </summary>
		/// <typeparam name="TFld"></typeparam>
		/// <param name="key"></param>
		/// <param name="field"></param>
		/// <param name="increment"></param>
		/// <returns></returns>
		Task<Bulk> HIncrByFloatAsync<TFld>(string key, TFld field, double increment);
		/// <summary>
		/// Increment the float value of a hash field by the given amount. Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="field"></param>
		/// <param name="increment"></param>
		/// <returns></returns>
		Task<Bulk> HIncrByFloatAsync(string key, byte[] field, double increment);
		Task<MultiBulk> HKeysAsync(string key);
		Task<MultiBulk> HValsAsync(string key);
		Task<long> HLenAsync(string key);
		Task<MultiBulk> HMGetAsync<TFld>(string key, params TFld[] fields);
		Task<MultiBulk> HMGetAsync(string key, params byte[][] fields);
		Task<string> HMSetAsync(string key, params MSetArgs[] args);
		Task<RedisResponse> EvalAsync(string script, string[] keys, params object[] arguments);
		Task<RedisResponse> EvalAsync(string script, string[] keys, params byte[][] arguments);
		Task<RedisResponse> EvalShaAsync(byte[] sha1, string[] keys, params object[] arguments);
		Task<RedisResponse> EvalShaAsync(byte[] sha1, string[] keys, params byte[][] arguments);
		Task<Bulk> ScriptLoadAsync(string script);
		Task<MultiBulk> ScriptExistsAsync(params byte[][] sha1);
		Task<string> ScriptFlushAsync();
		Task<string> ScriptKillAsync();
		Task<long> ZAddAsync<T>(string key, long score, T member);
		Task<long> ZAddAsync(string key, long score, byte[] member);
		Task<long> ZAddAsync<T>(string key, double score, T member);
		Task<long> ZAddAsync(string key, double score, byte[] member);
		Task<long> ZAddAsync(string key, params ZAddArgs[] args);
		Task<long> ZRemAsync<T>(string key, params T[] members);
		Task<long> ZRemAsync(string key, params object[] members);
		Task<long> ZRemAsync(string key, params byte[][] members);
		Task<long> ZRemRangeByRankAsync(string key, long start, long stop);
		Task<long> ZRemRangeByScoreAsync(string key, long min, long max);
		Task<long> ZRemRangeByScoreAsync(string key, double min, double max);
		Task<long> ZCardAsync(string key);
		Task<long> ZCountAsync(string key, long min, long max);
		Task<long> ZCountAsync(string key, double min, double max);
		Task<double> ZIncrByAsync<T>(string key, long increment, T member);
		Task<double> ZIncrByAsync(string key, long increment, byte[] member);
		Task<double> ZIncrByAsync<T>(string key, double increment, T member);
		Task<double> ZIncrByAsync(string key, double increment, byte[] member);
		Task<long> ZInterStoreAsync(string destinationKey, params string[] keys);
		Task<long> ZInterStoreAsync(string destinationKey, Aggregation aggregation, params string[] keys);
		Task<long> ZInterStoreAsync(string destinationKey, params ZAggrStoreArgs[] keys);
		Task<long> ZInterStoreAsync(string destinationKey, Aggregation aggregation, params ZAggrStoreArgs[] keys);
		Task<MultiBulk> ZRangeAsync(string key, long start, long stop, bool withScores = false);
		Task<MultiBulk> ZRangeByScoreAsync(string key, long min, long max, bool withScores = false);
		Task<MultiBulk> ZRangeByScoreAsync(string key, long min, long max, long limitOffset, long limitCount, bool withScores = false);
		Task<MultiBulk> ZRangeByScoreAsync(string key, double min, double max, bool withScores = false);
		Task<MultiBulk> ZRangeByScoreAsync(string key, double min, double max, long limitOffset, long limitCount, bool withScores = false);
		Task<long?> ZRankAsync<T>(string key, T member);
		Task<long?> ZRankAsync(string key, byte[] member);
		Task<long?> ZRevRankAsync<T>(string key, T member);
		Task<long?> ZRevRankAsync(string key, byte[] member);
		Task<MultiBulk> ZRevRangeAsync(string key, long start, long stop, bool withscores = false);
		Task<MultiBulk> ZRevRangeByScoreAsync(string key, long min, long max, bool withScores = false);
		Task<MultiBulk> ZRevRangeByScoreAsync(string key, long min, long max, long limitOffset, long limitCount, bool withScores = false);
		Task<MultiBulk> ZRevRangeByScoreAsync(string key, double min, double max, bool withScores = false);
		Task<MultiBulk> ZRevRangeByScoreAsync(string key, double min, double max, long limitOffset, long limitCount, bool withScores = false);
		Task<double> ZScoreAsync<T>(string key, T member);
		Task<double> ZScoreAsync(string key, byte[] member);
		Task<long> ZUnionStoreAsync(string destinationKey, params string[] keys);
		Task<long> ZUnionStoreAsync(string destinationKey, Aggregation aggregation, params string[] keys);
		Task<long> ZUnionStoreAsync(string destinationKey, params ZAggrStoreArgs[] keys);
		Task<long> ZUnionStoreAsync(string destinationKey, Aggregation aggregation, params ZAggrStoreArgs[] keys);
		Task<Bulk> GetAsync(string key);
		Task<string> SetAsync<T>(string key, T value);
		Task<string> SetAsync(string key, byte[] value);
		Task<long> AppendAsync<T>(string key, T value);
		Task<long> AppendAsync(string key, byte[] value);
		Task<long> BitCountAsync(string key);
		Task<long> BitCountAsync(string key, int start, int end);
		Task<long> DecrAsync(string key);
		Task<long> IncrAsync(string key);
		Task<long> DecrByAsync(string key, int decrement);
		Task<long> IncrByAsync(string key, int increment);
		Task<Bulk> IncrByFloatAsync(string key, double increment);
		Task<Bulk> GetRangeAsync(string key, int start, int end);
		Task<long> SetRangeAsync<T>(string key, int offset, T value);
		Task<long> SetRangeAsync(string key, int offset, byte[] value);
		Task<long> StrLenAsync(string key);
		Task<Bulk> GetSetAsync<T>(string key, T value);
		Task<Bulk> GetSetAsync(string key, byte[] value);
		Task<MultiBulk> MGetAsync(params string[] keys);
		Task<string> MSetAsync(params MSetArgs[] args);
		Task<long> MSetNxAsync(params MSetArgs[] args);
		Task<string> SetExAsync<T>(string key, int seconds, T value);
		Task<string> SetExAsync(string key, int seconds, byte[] value);
		Task<string> PSetExAsync<T>(string key, int milliseconds, T value);
		Task<string> PSetExAsync(string key, int milliseconds, byte[] value);
		Task<long> SetNxAsync<T>(string key, T value);
		Task<long> SetNxAsync(string key, byte[] value);
		Task<long> SAddAsync(string key, params object[] members);
		Task<long> SAddAsync<T>(string key, params T[] members);
		Task<long> SAddAsync(string key, params byte[][] members);
		Task<long> SRemAsync(string key, params object[] members);
		Task<long> SRemAsync<T>(string key, params T[] members);
		Task<long> SRemAsync(string key, params byte[][] members);
		Task<long> SCardAsync(string key);
		Task<MultiBulk> SDiffAsync(params string[] keys);
		Task<long> SDiffStoreAsync(string destinationKey, params string[] keys);
		Task<MultiBulk> SUnionAsync(params string[] keys);
		Task<long> SUnionStoreAsync(string destinationKey, params string[] keys);
		Task<MultiBulk> SInterAsync(params string[] keys);
		Task<long> SInterStoreAsync(string destinationKey, params string[] keys);
		Task<long> SIsMemberAsync<T>(string key, T value);
		Task<long> SIsMemberAsync(string key, byte[] value);
		Task<MultiBulk> SMembersAsync(string key);
		Task<long> SMoveAsync<T>(string sourceKey, string destinationKey, T member);
		Task<long> SMoveAsync(string sourceKey, string destinationKey, byte[] member);
		Task<Bulk> SPopAsync(string key);
		Task<Bulk> SRandMemberAsync(string key);
		Task<MultiBulk> SRandMemberAsync(string key, int count);
		Task<MultiBulk> BlPopAsync(int timeoutInSeconds, params string[] keys);
		Task<long> LPushAsync(string key, params object[] values);
		Task<long> LPushAsync<T>(string key, params T[] values);
		Task<long> LPushAsync(string key, params byte[][] values);
		Task<MultiBulk> BrPopAsync(int timeoutInSeconds, params string[] keys);
		Task<long> RPushAsync(string key, params object[] values);
		Task<long> RPushAsync<T>(string key, params T[] values);
		Task<long> RPushAsync(string key, params byte[][] values);
		Task<Bulk> LPopAsync(string key);
		Task<Bulk> RPopAsync(string key);
		Task<Bulk> RPopLPushAsync(string source, string destination);
		Task<Bulk> BRPopLPushAsync(string sourceKey, string destinationKey, int timeoutInSeconds);
		Task<Bulk> LIndexAsync(string key, int index);
		Task<long> LInsertAsync<TPivot,TValue>(string key, TPivot pivot, TValue value, bool before = true);
		Task<long> LInsertAsync(string key, byte[] pivot, byte[] value, bool before = true);
		Task<long> LLenAsync(string key);
		Task<long> LPushXAsync<T>(string key, T value);
		Task<long> LPushXAsync(string key, byte[] value);
		Task<MultiBulk> LRangeAsync(string key, int start, int stop);
		Task<long> LRemAsync<T>(string key, int count, T value);
		Task<long> LRemAsync(string key, int count, byte[] value);
		Task<string> LSetAsync<T>(string key, int index, T value);
		Task<string> LSetAsync(string key, int index, byte[] value);
		Task<string> LTrimAsync(string key, int start, int stop);
		Task<long> RPushXAsync<T>(string key, T values);
		Task<long> RPushXAsync(string key, byte[] values);
		Task<string> DiscardAsync();
		Task<MultiBulk> ExecAsync();
		Task<string> MultiAsync();
		Task<string> UnwatchAsync();
		Task<string> WatchAsync(params string[] keys);
		Task<long> PublishAsync<T>(string channel, T message);
		Task<long> PublishAsync(string channel, byte[] message);

		Task<IRedisSubscription> SubscribeAsync(params string[] channels);
		Task<IRedisSubscription> PSubscribeAsync(params string[] pattern);

	}
}
