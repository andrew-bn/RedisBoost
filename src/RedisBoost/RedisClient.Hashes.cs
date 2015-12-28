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

using System.Threading.Tasks;
using RedisBoost.Misk;
using RedisBoost.Core;

namespace RedisBoost
{
	public partial class RedisClient
	{
		public Task<long> HSetAsync<TFld,TVal>(string key, TFld field, TVal value)
		{
			return HSetAsync(key, Serialize(field), Serialize(value));
		}

		public Task<long> HSetAsync(string key, byte[] field, byte[] value)
		{
			return IntegerCommand(RedisConstants.HSet, key.ToBytes(), field, value);
		}

		public Task<long> HSetNxAsync<TFld, TVal>(string key, TFld field, TVal value)
		{
			return HSetNxAsync(key, Serialize(field), Serialize(value));
		}

		public Task<long> HSetNxAsync(string key, byte[] field, byte[] value)
		{
			return IntegerCommand(RedisConstants.HSetNx, key.ToBytes(), field, value);
		}

		public Task<long> HExistsAsync<TFld>(string key, TFld field)
		{
			return HExistsAsync(key, Serialize(field));
		}

		public Task<long> HExistsAsync(string key, byte[] field)
		{
			return IntegerCommand(RedisConstants.HExists, key.ToBytes(), field);
		}

		public Task<long> HDelAsync(string key, params object[] fields)
		{
			return HDelAsync(key, Serialize(fields));
		}

		public Task<long> HDelAsync<TFld>(string key, params TFld[] fields)
		{
			return HDelAsync(key, Serialize(fields));
		}

		public Task<long> HDelAsync(string key, params byte[][] fields)
		{
			var request = ComposeRequest(RedisConstants.HDel, key.ToBytes(), fields);
			return IntegerCommand(request);
		}

		public Task<Bulk> HGetAsync<TFld>(string key, TFld field)
		{
			return HGetAsync(key, Serialize(field));
		}

		public Task<Bulk> HGetAsync(string key, byte[] field)
		{
			return BulkCommand(RedisConstants.HGet, key.ToBytes(), field);
		}

		public Task<MultiBulk> HGetAllAsync(string key)
		{
			return MultiBulkCommand(RedisConstants.HGetAll, key.ToBytes());
		}

		public Task<long> HIncrByAsync<TFld>(string key, TFld field, long increment)
		{
			return HIncrByAsync(key, Serialize(field), increment);
		}

		public Task<long> HIncrByAsync(string key, byte[] field, long increment)
		{
			return IntegerCommand(RedisConstants.HIncrBy, key.ToBytes(), field, increment.ToBytes());
		}

		public Task<Bulk> HIncrByFloatAsync<TFld>(string key, TFld field, double increment)
		{
			return HIncrByFloatAsync(key, Serialize(field), increment);
		}

		public Task<Bulk> HIncrByFloatAsync(string key, byte[] field, double increment)
		{
			return BulkCommand(RedisConstants.HIncrByFloat, key.ToBytes(),
				field, increment.ToBytes());
		}

		public Task<MultiBulk> HKeysAsync(string key)
		{
			return MultiBulkCommand(RedisConstants.HKeys, key.ToBytes());
		}

		public Task<MultiBulk> HValsAsync(string key)
		{
			return MultiBulkCommand(RedisConstants.HVals, key.ToBytes());
		}

		public Task<long> HLenAsync(string key)
		{
			return IntegerCommand(RedisConstants.HLen, key.ToBytes());
		}

		public Task<MultiBulk> HMGetAsync<TFld>(string key, params TFld[] fields)
		{
			return HMGetAsync(key, Serialize(fields));
		}

		public Task<MultiBulk> HMGetAsync(string key, params byte[][] fields)
		{
			var request = ComposeRequest(RedisConstants.HMGet, key.ToBytes(), fields);
			return MultiBulkCommand(request);
		}

		public Task<string> HMSetAsync(string key,params MSetArgs[] args)
		{
			var request = ComposeRequest(RedisConstants.HMSet, key.ToBytes(), args);
			return StatusCommand(request);
		}


	}
}
