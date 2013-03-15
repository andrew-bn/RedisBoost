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

using System.Threading.Tasks;
using NBoosters.RedisBoost.Core;

namespace NBoosters.RedisBoost
{
	public partial class RedisClient
	{
		public Task<long> HSetAsync<TFld,TVal>(string key, TFld field, TVal value)
		{
			return HSetAsync(key, Serialize(field), Serialize(value));
		}

		public Task<long> HSetAsync(string key, byte[] field, byte[] value)
		{
			return IntegerResponseCommand(RedisConstants.HSet, ConvertToByteArray(key),field,value);
		}
		public Task<long> HSetNxAsync<TFld, TVal>(string key, TFld field, TVal value)
		{
			return HSetNxAsync(key, Serialize(field), Serialize(value));
		}

		public Task<long> HSetNxAsync(string key, byte[] field, byte[] value)
		{
			return IntegerResponseCommand(RedisConstants.HSetNx, ConvertToByteArray(key), field, value);
		}
		public Task<long> HExistsAsync<TFld>(string key, TFld field)
		{
			return HExistsAsync(key, Serialize(field));
		}

		public Task<long> HExistsAsync(string key, byte[] field)
		{
			return IntegerResponseCommand(RedisConstants.HExists, ConvertToByteArray(key), field);
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
			if (fields.Length == 0)
				throw new RedisException("Fields array is empty");

			var request = new byte[fields.Length + 2][];
			request[0] = RedisConstants.HDel;
			request[1] = ConvertToByteArray(key);
			for (int i = 0; i < fields.Length; i++)
				request[i + 2] = fields[i];
			return IntegerResponseCommand(request);
		}

		public Task<Bulk> HGetAsync<TFld>(string key, TFld field)
		{
			return HGetAsync(key, Serialize(field));
		}

		public Task<Bulk> HGetAsync(string key, byte[] field)
		{
			return BulkResponseCommand(RedisConstants.HGet, ConvertToByteArray(key), field);
		}

		public Task<MultiBulk> HGetAllAsync(string key)
		{
			return MultiBulkResponseCommand(RedisConstants.HGetAll, ConvertToByteArray(key));
		}

		public Task<long> HIncrByAsync<TFld>(string key, TFld field, int increment)
		{
			return HIncrByAsync(key, Serialize(field), increment);
		}
		public Task<long> HIncrByAsync(string key, byte[] field, int increment)
		{
			return IntegerResponseCommand(RedisConstants.HIncrBy, ConvertToByteArray(key),
				field,ConvertToByteArray(increment));
		}
		public Task<Bulk> HIncrByFloatAsync<TFld>(string key, TFld field, double increment)
		{
			return HIncrByFloatAsync(key, Serialize(field), increment);
		}
		public Task<Bulk> HIncrByFloatAsync(string key, byte[] field, double increment)
		{
			return BulkResponseCommand(RedisConstants.HIncrByFloat, ConvertToByteArray(key),
				field, ConvertToByteArray(increment));
		}

		public Task<MultiBulk> HKeysAsync(string key)
		{
			return MultiBulkResponseCommand(RedisConstants.HKeys, ConvertToByteArray(key));
		}
		public Task<MultiBulk> HValsAsync(string key)
		{
			return MultiBulkResponseCommand(RedisConstants.HVals, ConvertToByteArray(key));
		}
		public Task<long> HLenAsync(string key)
		{
			return IntegerResponseCommand(RedisConstants.HLen, ConvertToByteArray(key));
		}

		public Task<MultiBulk> HMGetAsync<TFld>(string key, params TFld[] fields)
		{
			return HMGetAsync(key, Serialize(fields));
		}

		public Task<MultiBulk> HMGetAsync(string key, params byte[][] fields)
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
				var arg = args[i];
				request[i * 2 + 2] = ConvertToByteArray(arg.KeyOrField);
				request[i * 2 + 3] = arg.IsArray ? (byte[])arg.Value : Serialize(arg.Value);
			}
			return StatusResponseCommand(request);
		}
	}
}
