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
		/// <br/> Complexity: O(N) where N is the number of client connections
		/// </summary>
		/// <param name="ip"></param>
		/// <param name="port"></param>
		/// <returns></returns>
		Task<string> ClientKillAsync(string ip, int port);
		/// <summary>
		/// Find all keys matching the given pattern.
		/// <br/> Complexity: O(N) with N being the number of keys in the database, under the assumption that the key names in the database and the given pattern have limited length.
		/// </summary>
		/// <param name="pattern"></param>
		/// <returns></returns>
		Task<MultiBulk> KeysAsync(string pattern);
		/// <summary>
		/// Delete a key.
		/// <br/> Complexity: O(N) where N is the number of keys that will be removed. When a key to remove holds a value other than a string, the individual <br/> Complexity for this key is O(M) where M is the number of elements in the list, set, sorted set or hash. Removing a single key that holds a string value is O(1).
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		Task<long> DelAsync(string key);
		/// <summary>
		/// Perform bitwise operations between strings.
		/// <br/> Complexity: O(N)
		/// </summary>
		/// <param name="bitOp"></param>
		/// <param name="destKey"></param>
		/// <param name="keys"></param>
		/// <returns></returns>
		Task<long> BitOpAsync(BitOpType bitOp, string destKey, params string[] keys);
		/// <summary>
		/// Sets or clears the bit at offset in the string value stored at key. <br/> Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="offset"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		Task<long> SetBitAsync(string key, long offset, int value);
		/// <summary>
		/// Returns the bit value at offset in the string value stored at key. <br/> Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="offset"></param>
		/// <returns></returns>
		Task<long> GetBitAsync(string key, long offset);
		/// <summary>
		/// Atomically transfer a key from a Redis instance to another one. 
		/// <br/> Complexity: This command actually executes a DUMP+DEL in the source instance, and a RESTORE in the target instance. See the pages of these commands for time <br/> Complexity. Also an O(N) data transfer between the two instances is performed.
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
		/// <br/> Complexity: O(1) to access the key and additional O(N*M) to serialized it, where N is the number of Redis objects composing the value and M their average size. For small string values the time <br/> Complexity is thus O(1)+O(1*M) where M is small, so simply O(1).
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		Task<Bulk> DumpAsync(string key);
		/// <summary>
		/// Move a key to another database. <br/> Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="db"></param>
		/// <returns></returns>
		Task<long> MoveAsync(string key, int db);
		/// <summary>
		/// Inspect the internals of Redis objects. <br/> Complexity: O(1) for all the currently implemented subcommands.
		/// </summary>
		/// <param name="subcommand"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		Task<RedisResponse> ObjectAsync(Subcommand subcommand, params string[] args);
		/// <summary>
		/// Sort the elements in a list, set or sorted set.
		/// <br/> Complexity: O(N+M*log(M)) where N is the number of elements in the list or set to sort, and M the number of returned elements. When the elements are not sorted, <br/> Complexity is currently O(N) as there is a copy step that will be avoided in next releases.
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
		/// <br/> Complexity: O(1) to create the new key and additional O(N*M) to recostruct the serialized value, where N is the number of Redis objects composing the value and M their average size. For small string values the time <br/> Complexity is thus O(1)+O(1*M) where M is small, so simply O(1). However for sorted set values the <br/> Complexity is O(N*M*log(N)) because inserting values into sorted sets is O(log(N)).
		/// </summary>
		/// <param name="key"></param>
		/// <param name="ttlInMilliseconds"></param>
		/// <param name="serializedValue"></param>
		/// <returns></returns>
		Task<string> RestoreAsync(string key, int ttlInMilliseconds, byte[] serializedValue);
		/// <summary>
		/// Determine if a key exists.
		/// <br/> Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		Task<long> ExistsAsync(string key);
		/// <summary>
		/// Set a key's time to live in seconds.
		/// <br/> Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="seconds"></param>
		/// <returns></returns>
		Task<long> ExpireAsync(string key, int seconds);
		/// <summary>
		/// Set a key's time to live in milliseconds.
		/// <br/> Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="milliseconds"></param>
		/// <returns></returns>
		Task<long> PExpireAsync(string key, int milliseconds);
		/// <summary>
		/// Set a key's time to live in seconds.
		/// <br/> Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="timestamp"></param>
		/// <returns></returns>
		Task<long> ExpireAtAsync(string key, DateTime timestamp);
		/// <summary>
		/// Remove the expiration from a key. <br/> Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		Task<long> PersistAsync(string key);
		/// <summary>
		/// Get the time to live for a key in milliseconds. <br/> Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		Task<long> PttlAsync(string key);
		/// <summary>
		/// Get the time to live for a key. <br/> Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		Task<long> TtlAsync(string key);
		/// <summary>
		/// Returns the string representation of the type of the value stored at key. The different types that can be returned are: string, list, set, zset and hash.
		/// <br/> Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		Task<string> TypeAsync(string key);
		/// <summary>
		/// Return a random key from the keyspace. <br/> Complexity: O(1)
		/// </summary>
		/// <returns></returns>
		Task<string> RandomKeyAsync();
		/// <summary>
		/// Rename a key. <br/> Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="newKey"></param>
		/// <returns></returns>
		Task<string> RenameAsync(string key, string newKey);
		/// <summary>
		/// Rename a key, only if the new key does not exist.
		/// <br/> Complexity: O(1)
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
		/// Get the list of client connections. <br/> Complexity: O(N) where N is the number of client connections.
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
		/// Reset the stats returned by INFO. <br/> Complexity: O(1)
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
		/// Return the current server time. <br/> Complexity: O(1)
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
		/// Set the string value of a hash field. <br/> Complexity: O(1)
		/// </summary>
		/// <typeparam name="TFld"></typeparam>
		/// <typeparam name="TVal"></typeparam>
		/// <param name="key"></param>
		/// <param name="field"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		Task<long> HSetAsync<TFld,TVal>(string key, TFld field, TVal value);
		/// <summary>
		/// Set the string value of a hash field. <br/> Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="field"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		Task<long> HSetAsync(string key, byte[] field, byte[] value);
		/// <summary>
		/// Set the value of a hash field, only if the field does not exist. <br/> Complexity: O(1)
		/// </summary>
		/// <typeparam name="TFld"></typeparam>
		/// <typeparam name="TVal"></typeparam>
		/// <param name="key"></param>
		/// <param name="field"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		Task<long> HSetNxAsync<TFld,TVal>(string key, TFld field, TVal value);
		/// <summary>
		/// Set the value of a hash field, only if the field does not exist. <br/> Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="field"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		Task<long> HSetNxAsync(string key, byte[] field, byte[] value);
		/// <summary>
		/// Determine if a hash field exists. <br/> Complexity: O(1)
		/// </summary>
		/// <typeparam name="TFld"></typeparam>
		/// <param name="key"></param>
		/// <param name="field"></param>
		/// <returns></returns>
		Task<long> HExistsAsync<TFld>(string key, TFld field);
		/// <summary>
		/// Determine if a hash field exists. <br/> Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="field"></param>
		/// <returns></returns>
		Task<long> HExistsAsync(string key, byte[] field);
		/// <summary>
		/// Delete one or more hash fields. <br/> Complexity: O(N) where N is the number of fields to be removed.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="fields"></param>
		/// <returns></returns>
		Task<long> HDelAsync(string key, params object[] fields);
		/// <summary>
		/// Delete one or more hash fields. <br/> Complexity: O(N) where N is the number of fields to be removed.
		/// </summary>
		/// <typeparam name="TFld"></typeparam>
		/// <param name="key"></param>
		/// <param name="fields"></param>
		/// <returns></returns>
		Task<long> HDelAsync<TFld>(string key, params TFld[] fields);
		/// <summary>
		/// Delete one or more hash fields. <br/> Complexity: O(N) where N is the number of fields to be removed.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="fields"></param>
		/// <returns></returns>
		Task<long> HDelAsync(string key, params byte[][] fields);
		/// <summary>
		/// Get the value of a hash field. <br/> Complexity: O(1)
		/// </summary>
		/// <typeparam name="TFld"></typeparam>
		/// <param name="key"></param>
		/// <param name="field"></param>
		/// <returns></returns>
		Task<Bulk> HGetAsync<TFld>(string key, TFld field);
		/// <summary>
		/// Get the value of a hash field. <br/> Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="field"></param>
		/// <returns></returns>
		Task<Bulk> HGetAsync(string key, byte[] field);
		/// <summary>
		/// Get all the fields and values in a hash. <br/> Complexity: O(N) where N is the size of the hash.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		Task<MultiBulk> HGetAllAsync(string key);
		/// <summary>
		/// Increment the integer value of a hash field by the given number. <br/> Complexity: O(1)
		/// </summary>
		/// <typeparam name="TFld"></typeparam>
		/// <param name="key"></param>
		/// <param name="field"></param>
		/// <param name="increment"></param>
		/// <returns></returns>
		Task<long> HIncrByAsync<TFld>(string key, TFld field, int increment);
		/// <summary>
		/// Increment the integer value of a hash field by the given number. <br/> Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="field"></param>
		/// <param name="increment"></param>
		/// <returns></returns>
		Task<long> HIncrByAsync(string key, byte[] field, int increment);
		/// <summary>
		/// Increment the float value of a hash field by the given amount. <br/> Complexity: O(1)
		/// </summary>
		/// <typeparam name="TFld"></typeparam>
		/// <param name="key"></param>
		/// <param name="field"></param>
		/// <param name="increment"></param>
		/// <returns></returns>
		Task<Bulk> HIncrByFloatAsync<TFld>(string key, TFld field, double increment);
		/// <summary>
		/// Increment the float value of a hash field by the given amount. <br/> Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="field"></param>
		/// <param name="increment"></param>
		/// <returns></returns>
		Task<Bulk> HIncrByFloatAsync(string key, byte[] field, double increment);
		/// <summary>
		/// Get all the fields in a hash. <br/> Complexity: O(N) where N is the size of the hash.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		Task<MultiBulk> HKeysAsync(string key);
		/// <summary>
		/// Get all the values in a hash. <br/> Complexity: O(N) where N is the size of the hash.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		Task<MultiBulk> HValsAsync(string key);
		/// <summary>
		/// Get the number of fields in a hash. <br/> Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		Task<long> HLenAsync(string key);
		/// <summary>
		/// Get the values of all the given hash fields. <br/> Complexity: O(N) where N is the number of fields being requested.
		/// </summary>
		/// <typeparam name="TFld"></typeparam>
		/// <param name="key"></param>
		/// <param name="fields"></param>
		/// <returns></returns>
		Task<MultiBulk> HMGetAsync<TFld>(string key, params TFld[] fields);
		/// <summary>
		/// Get the values of all the given hash fields. <br/> Complexity: O(N) where N is the number of fields being requested.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="fields"></param>
		/// <returns></returns>
		Task<MultiBulk> HMGetAsync(string key, params byte[][] fields);
		/// <summary>
		/// Set multiple hash fields to multiple values. <br/> Complexity: O(N) where N is the number of fields being set.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		Task<string> HMSetAsync(string key, params MSetArgs[] args);
		/// <summary>
		/// Execute a Lua script server side. <br/> Complexity: Depends on the script that is executed.
		/// </summary>
		/// <param name="script"></param>
		/// <param name="keys"></param>
		/// <param name="arguments"></param>
		/// <returns></returns>
		Task<RedisResponse> EvalAsync(string script, string[] keys, params object[] arguments);
		/// <summary>
		/// Execute a Lua script server side. <br/> Complexity: Depends on the script that is executed.
		/// </summary>
		/// <param name="script"></param>
		/// <param name="keys"></param>
		/// <param name="arguments"></param>
		/// <returns></returns>
		Task<RedisResponse> EvalAsync(string script, string[] keys, params byte[][] arguments);
		/// <summary>
		/// Execute a Lua script server side. <br/> Complexity: Depends on the script that is executed.
		/// </summary>
		/// <param name="sha1"></param>
		/// <param name="keys"></param>
		/// <param name="arguments"></param>
		/// <returns></returns>
		Task<RedisResponse> EvalShaAsync(byte[] sha1, string[] keys, params object[] arguments);
		/// <summary>
		/// Execute a Lua script server side. <br/> Complexity: Depends on the script that is executed.
		/// </summary>
		/// <param name="sha1"></param>
		/// <param name="keys"></param>
		/// <param name="arguments"></param>
		/// <returns></returns>
		Task<RedisResponse> EvalShaAsync(byte[] sha1, string[] keys, params byte[][] arguments);
		/// <summary>
		/// Load the specified Lua script into the script cache. <br/> Complexity: O(N) with N being the length in bytes of the script body.
		/// </summary>
		/// <param name="script"></param>
		/// <returns></returns>
		Task<Bulk> ScriptLoadAsync(string script);
		/// <summary>
		/// Check existence of scripts in the script cache. <br/> Complexity: O(N) with N being the number of scripts to check (so checking a single script is an O(1) operation).
		/// </summary>
		/// <param name="sha1"></param>
		/// <returns></returns>
		Task<MultiBulk> ScriptExistsAsync(params byte[][] sha1);
		/// <summary>
		/// Remove all the scripts from the script cache. <br/> Complexity: O(N) with N being the number of scripts in cache.
		/// </summary>
		/// <returns></returns>
		Task<string> ScriptFlushAsync();
		/// <summary>
		/// Kill the script currently in execution. <br/> Complexity: O(1)
		/// </summary>
		/// <returns></returns>
		Task<string> ScriptKillAsync();
		/// <summary>
		/// Add one or more members to a sorted set, or update its score if it already exists.
		/// <br/> Complexity: O(log(N)) where N is the number of elements in the sorted set.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="score"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		Task<long> ZAddAsync<T>(string key, long score, T member);
		/// <summary>
		/// Add one or more members to a sorted set, or update its score if it already exists.
		/// <br/> Complexity: O(log(N)) where N is the number of elements in the sorted set.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="score"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		Task<long> ZAddAsync(string key, long score, byte[] member);
		/// <summary>
		/// Add one or more members to a sorted set, or update its score if it already exists.
		/// <br/> Complexity: O(log(N)) where N is the number of elements in the sorted set.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="score"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		Task<long> ZAddAsync<T>(string key, double score, T member);
		/// <summary>
		/// Add one or more members to a sorted set, or update its score if it already exists.
		/// <br/> Complexity: O(log(N)) where N is the number of elements in the sorted set.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="score"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		Task<long> ZAddAsync(string key, double score, byte[] member);
		/// <summary>
		/// Add one or more members to a sorted set, or update its score if it already exists.
		/// <br/> Complexity: O(log(N)) where N is the number of elements in the sorted set.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		Task<long> ZAddAsync(string key, params ZAddArgs[] args);
		/// <summary>
		/// Remove one or more members from a sorted set.
		/// <br/> Complexity: O(M*log(N)) with N being the number of elements in the sorted set and M the number of elements to be removed.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="members"></param>
		/// <returns></returns>
		Task<long> ZRemAsync<T>(string key, params T[] members);
		/// <summary>
		/// Remove one or more members from a sorted set.
		/// <br/> Complexity: O(M*log(N)) with N being the number of elements in the sorted set and M the number of elements to be removed.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="members"></param>
		/// <returns></returns>
		Task<long> ZRemAsync(string key, params object[] members);
		/// <summary>
		/// Remove one or more members from a sorted set.
		/// <br/> Complexity: O(M*log(N)) with N being the number of elements in the sorted set and M the number of elements to be removed.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="members"></param>
		/// <returns></returns>
		Task<long> ZRemAsync(string key, params byte[][] members);
		/// <summary>
		/// Remove all members in a sorted set within the given indexes.
		/// <br/> Complexity: O(log(N)+M) with N being the number of elements in the sorted set and M the number of elements removed by the operation.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="start"></param>
		/// <param name="stop"></param>
		/// <returns></returns>
		Task<long> ZRemRangeByRankAsync(string key, long start, long stop);
		/// <summary>
		/// Remove all members in a sorted set within the given scores.
		/// <br/> Complexity: O(log(N)+M) with N being the number of elements in the sorted set and M the number of elements removed by the operation.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		Task<long> ZRemRangeByScoreAsync(string key, long min, long max);
		/// <summary>
		/// Remove all members in a sorted set within the given scores.
		/// <br/> Complexity: O(log(N)+M) with N being the number of elements in the sorted set and M the number of elements removed by the operation.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		Task<long> ZRemRangeByScoreAsync(string key, double min, double max);
		/// <summary>
		/// Get the number of members in a sorted set. <br/> Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		Task<long> ZCardAsync(string key);
		/// <summary>
		/// Count the members in a sorted set with scores within the given values.
		/// <br/> Complexity: O(log(N)+M) with N being the number of elements in the sorted set and M being the number of elements between min and max.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		Task<long> ZCountAsync(string key, long min, long max);
		/// <summary>
		/// Count the members in a sorted set with scores within the given values.
		/// <br/> Complexity: O(log(N)+M) with N being the number of elements in the sorted set and M being the number of elements between min and max.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		Task<long> ZCountAsync(string key, double min, double max);
		/// <summary>
		/// Increment the score of a member in a sorted set.
		/// <br/> Complexity: O(log(N)) where N is the number of elements in the sorted set.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="increment"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		Task<double> ZIncrByAsync<T>(string key, long increment, T member);
		/// <summary>
		/// Increment the score of a member in a sorted set.
		/// <br/> Complexity: O(log(N)) where N is the number of elements in the sorted set.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="increment"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		Task<double> ZIncrByAsync(string key, long increment, byte[] member);
		/// <summary>
		/// Increment the score of a member in a sorted set.
		/// <br/> Complexity: O(log(N)) where N is the number of elements in the sorted set.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="increment"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		Task<double> ZIncrByAsync<T>(string key, double increment, T member);
		/// <summary>
		/// Increment the score of a member in a sorted set.
		/// <br/> Complexity: O(log(N)) where N is the number of elements in the sorted set.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="increment"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		Task<double> ZIncrByAsync(string key, double increment, byte[] member);
		/// <summary>
		/// Intersect multiple sorted sets and store the resulting sorted set in a new key.
		/// <br/> Complexity: O(N*K)+O(M*log(M)) worst case with N being the smallest input sorted set, K being the number of input sorted sets and M being the number of elements in the resulting sorted set.
		/// </summary>
		/// <param name="destinationKey"></param>
		/// <param name="keys"></param>
		/// <returns></returns>
		Task<long> ZInterStoreAsync(string destinationKey, params string[] keys);
		/// <summary>
		/// Intersect multiple sorted sets and store the resulting sorted set in a new key.
		/// <br/> Complexity: O(N*K)+O(M*log(M)) worst case with N being the smallest input sorted set, K being the number of input sorted sets and M being the number of elements in the resulting sorted set.
		/// </summary>
		/// <param name="destinationKey"></param>
		/// <param name="aggregation"></param>
		/// <param name="keys"></param>
		/// <returns></returns>
		Task<long> ZInterStoreAsync(string destinationKey, Aggregation aggregation, params string[] keys);
		/// <summary>
		/// Intersect multiple sorted sets and store the resulting sorted set in a new key.
		/// <br/> Complexity: O(N*K)+O(M*log(M)) worst case with N being the smallest input sorted set, K being the number of input sorted sets and M being the number of elements in the resulting sorted set.
		/// </summary>
		/// <param name="destinationKey"></param>
		/// <param name="keys"></param>
		/// <returns></returns>
		Task<long> ZInterStoreAsync(string destinationKey, params ZAggrStoreArgs[] keys);
		/// <summary>
		/// Intersect multiple sorted sets and store the resulting sorted set in a new key.
		/// <br/> Complexity: O(N*K)+O(M*log(M)) worst case with N being the smallest input sorted set, K being the number of input sorted sets and M being the number of elements in the resulting sorted set.
		/// </summary>
		/// <param name="destinationKey"></param>
		/// <param name="aggregation"></param>
		/// <param name="keys"></param>
		/// <returns></returns>
		Task<long> ZInterStoreAsync(string destinationKey, Aggregation aggregation, params ZAggrStoreArgs[] keys);
		/// <summary>
		/// Return a range of members in a sorted set, by index.
		/// <br/> Complexity: O(log(N)+M) with N being the number of elements in the sorted set and M the number of elements returned.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="start"></param>
		/// <param name="stop"></param>
		/// <param name="withScores"></param>
		/// <returns></returns>
		Task<MultiBulk> ZRangeAsync(string key, long start, long stop, bool withScores = false);
		/// <summary>
		/// Return a range of members in a sorted set, by score.
		/// <br/> Complexity: O(log(N)+M) with N being the number of elements in the sorted set and M the number of elements being returned. If M is constant (e.g. always asking for the first 10 elements with LIMIT), you can consider it O(log(N)).
		/// </summary>
		/// <param name="key"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <param name="withScores"></param>
		/// <returns></returns>
		Task<MultiBulk> ZRangeByScoreAsync(string key, long min, long max, bool withScores = false);
		/// <summary>
		/// Return a range of members in a sorted set, by score.
		/// <br/> Complexity: O(log(N)+M) with N being the number of elements in the sorted set and M the number of elements being returned. If M is constant (e.g. always asking for the first 10 elements with LIMIT), you can consider it O(log(N)).
		/// </summary>
		/// <param name="key"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <param name="limitOffset"></param>
		/// <param name="limitCount"></param>
		/// <param name="withScores"></param>
		/// <returns></returns>
		Task<MultiBulk> ZRangeByScoreAsync(string key, long min, long max, long limitOffset, long limitCount, bool withScores = false);
		/// <summary>
		/// Return a range of members in a sorted set, by score.
		/// <br/> Complexity: O(log(N)+M) with N being the number of elements in the sorted set and M the number of elements being returned. If M is constant (e.g. always asking for the first 10 elements with LIMIT), you can consider it O(log(N)).
		/// </summary>
		/// <param name="key"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <param name="withScores"></param>
		/// <returns></returns>
		Task<MultiBulk> ZRangeByScoreAsync(string key, double min, double max, bool withScores = false);
		/// <summary>
		/// Return a range of members in a sorted set, by score.
		/// <br/> Complexity: O(log(N)+M) with N being the number of elements in the sorted set and M the number of elements being returned. If M is constant (e.g. always asking for the first 10 elements with LIMIT), you can consider it O(log(N)).
		/// </summary>
		/// <param name="key"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <param name="limitOffset"></param>
		/// <param name="limitCount"></param>
		/// <param name="withScores"></param>
		/// <returns></returns>
		Task<MultiBulk> ZRangeByScoreAsync(string key, double min, double max, long limitOffset, long limitCount, bool withScores = false);
		/// <summary>
		/// Determine the index of a member in a sorted set.
		/// <br/> Complexity: O(log(N))
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		Task<long?> ZRankAsync<T>(string key, T member);
		/// <summary>
		/// Determine the index of a member in a sorted set.
		/// <br/> Complexity: O(log(N))
		/// </summary>
		/// <param name="key"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		Task<long?> ZRankAsync(string key, byte[] member);
		/// <summary>
		/// Determine the index of a member in a sorted set, with scores ordered from high to low.
		/// <br/> Complexity: O(log(N))
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		Task<long?> ZRevRankAsync<T>(string key, T member);
		/// <summary>
		/// Determine the index of a member in a sorted set, with scores ordered from high to low.
		/// <br/> Complexity: O(log(N))
		/// </summary>
		/// <param name="key"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		Task<long?> ZRevRankAsync(string key, byte[] member);
		/// <summary>
		/// Return a range of members in a sorted set, by index, with scores ordered from high to low.
		/// <br/> Complexity: O(log(N)+M) with N being the number of elements in the sorted set and M the number of elements returned.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="start"></param>
		/// <param name="stop"></param>
		/// <param name="withscores"></param>
		/// <returns></returns>
		Task<MultiBulk> ZRevRangeAsync(string key, long start, long stop, bool withscores = false);
		/// <summary>
		/// Return a range of members in a sorted set, by score, with scores ordered from high to low.
		/// <br/> Complexity: O(log(N)+M) with N being the number of elements in the sorted set and M the number of elements being returned. If M is constant (e.g. always asking for the first 10 elements with LIMIT), you can consider it O(log(N)).
		/// </summary>
		/// <param name="key"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <param name="withScores"></param>
		/// <returns></returns>
		Task<MultiBulk> ZRevRangeByScoreAsync(string key, long min, long max, bool withScores = false);
		/// <summary>
		/// Return a range of members in a sorted set, by score, with scores ordered from high to low.
		/// <br/> Complexity: O(log(N)+M) with N being the number of elements in the sorted set and M the number of elements being returned. If M is constant (e.g. always asking for the first 10 elements with LIMIT), you can consider it O(log(N)).
		/// </summary>
		/// <param name="key"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <param name="limitOffset"></param>
		/// <param name="limitCount"></param>
		/// <param name="withScores"></param>
		/// <returns></returns>
		Task<MultiBulk> ZRevRangeByScoreAsync(string key, long min, long max, long limitOffset, long limitCount, bool withScores = false);
		/// <summary>
		/// Return a range of members in a sorted set, by score, with scores ordered from high to low.
		/// <br/> Complexity: O(log(N)+M) with N being the number of elements in the sorted set and M the number of elements being returned. If M is constant (e.g. always asking for the first 10 elements with LIMIT), you can consider it O(log(N)).
		/// </summary>
		/// <param name="key"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <param name="withScores"></param>
		/// <returns></returns>
		Task<MultiBulk> ZRevRangeByScoreAsync(string key, double min, double max, bool withScores = false);
		/// <summary>
		/// Return a range of members in a sorted set, by score, with scores ordered from high to low.
		/// <br/> Complexity: O(log(N)+M) with N being the number of elements in the sorted set and M the number of elements being returned. If M is constant (e.g. always asking for the first 10 elements with LIMIT), you can consider it O(log(N)).
		/// </summary>
		/// <param name="key"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <param name="limitOffset"></param>
		/// <param name="limitCount"></param>
		/// <param name="withScores"></param>
		/// <returns></returns>
		Task<MultiBulk> ZRevRangeByScoreAsync(string key, double min, double max, long limitOffset, long limitCount, bool withScores = false);
		/// <summary>
		/// Get the score associated with the given member in a sorted set.
		/// <br/> Complexity: O(1)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		Task<double> ZScoreAsync<T>(string key, T member);
		/// <summary>
		/// Get the score associated with the given member in a sorted set.
		/// <br/> Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		Task<double> ZScoreAsync(string key, byte[] member);
		/// <summary>
		/// Add multiple sorted sets and store the resulting sorted set in a new key.
		/// <br/> Complexity: O(N)+O(M log(M)) with N being the sum of the sizes of the input sorted sets, and M being the number of elements in the resulting sorted set.
		/// </summary>
		/// <param name="destinationKey"></param>
		/// <param name="keys"></param>
		/// <returns></returns>
		Task<long> ZUnionStoreAsync(string destinationKey, params string[] keys);
		/// <summary>
		/// Add multiple sorted sets and store the resulting sorted set in a new key.
		/// <br/> Complexity: O(N)+O(M log(M)) with N being the sum of the sizes of the input sorted sets, and M being the number of elements in the resulting sorted set.
		/// </summary>
		/// <param name="destinationKey"></param>
		/// <param name="aggregation"></param>
		/// <param name="keys"></param>
		/// <returns></returns>
		Task<long> ZUnionStoreAsync(string destinationKey, Aggregation aggregation, params string[] keys);
		/// <summary>
		/// Add multiple sorted sets and store the resulting sorted set in a new key.
		/// <br/> Complexity: O(N)+O(M log(M)) with N being the sum of the sizes of the input sorted sets, and M being the number of elements in the resulting sorted set.
		/// </summary>
		/// <param name="destinationKey"></param>
		/// <param name="keys"></param>
		/// <returns></returns>
		Task<long> ZUnionStoreAsync(string destinationKey, params ZAggrStoreArgs[] keys);
		/// <summary>
		/// Add multiple sorted sets and store the resulting sorted set in a new key.
		/// <br/> Complexity: O(N)+O(M log(M)) with N being the sum of the sizes of the input sorted sets, and M being the number of elements in the resulting sorted set.
		/// </summary>
		/// <param name="destinationKey"></param>
		/// <param name="aggregation"></param>
		/// <param name="keys"></param>
		/// <returns></returns>
		Task<long> ZUnionStoreAsync(string destinationKey, Aggregation aggregation, params ZAggrStoreArgs[] keys);
		/// <summary>
		/// Get the value of a key.
		/// <br/> Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		Task<Bulk> GetAsync(string key);
		/// <summary>
		/// Set value of a key.
		/// <br/> Complexity: O(1)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		Task<string> SetAsync<T>(string key, T value);
		/// <summary>
		/// Set value of a key.
		/// <br/> Complexity: O(1)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
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
