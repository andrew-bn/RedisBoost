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
		public Task<MultiBulk> BlPopAsync(int timeoutInSeconds, params string[] keys)
		{
			var request = ComposeRequest(RedisConstants.BlPop, keys, ConvertToByteArray(timeoutInSeconds));
			return MultiBulkResponseCommand(request);
		}

		public Task<long> LPushAsync(string key, params object[] values)
		{
			return LPushAsync(key, Serialize(values));
		}

		public Task<long> LPushAsync<T>(string key, params T[] values)
		{
			return LPushAsync(key, Serialize(values));
		}

		public Task<long> LPushAsync(string key, params byte[][] values)
		{
			var request = new byte[values.Length + 2][];
			request[0] = RedisConstants.LPush;
			request[1] = ConvertToByteArray(key);
			for (int i = 0; i < values.Length; i++)
				request[i + 2] = values[i];

			return IntegerResponseCommand(request);
		}

		public Task<MultiBulk> BrPopAsync(int timeoutInSeconds, params string[] keys)
		{
			var request = ComposeRequest(RedisConstants.BrPop, keys, ConvertToByteArray(timeoutInSeconds));
			return MultiBulkResponseCommand(request);
		}

		public Task<long> RPushAsync(string key, params object[] values)
		{
			return RPushAsync(key, Serialize(values));
		}

		public Task<long> RPushAsync<T>(string key, params T[] values)
		{
			return RPushAsync(key, Serialize(values));
		}

		public Task<long> RPushAsync(string key, params byte[][] values)
		{
			var request = ComposeRequest(RedisConstants.RPush, ConvertToByteArray(key), values);
			return IntegerResponseCommand(request);
		}

		public Task<Bulk> LPopAsync(string key)
		{
			return BulkResponseCommand(RedisConstants.LPop, ConvertToByteArray(key));
		}

		public Task<Bulk> RPopAsync(string key)
		{
			return BulkResponseCommand(RedisConstants.RPop, ConvertToByteArray(key));
		}

		public Task<Bulk> RPopLPushAsync(string source, string destination)
		{
			return BulkResponseCommand(RedisConstants.RPopLPush, ConvertToByteArray(source),
				ConvertToByteArray(destination));
		}

		public Task<Bulk> BRPopLPushAsync(string sourceKey, string destinationKey, int timeoutInSeconds)
		{
			return BulkResponseCommand(RedisConstants.BRPopLPush, ConvertToByteArray(sourceKey),
				ConvertToByteArray(destinationKey),ConvertToByteArray(timeoutInSeconds));
		}

		public Task<Bulk> LIndexAsync(string key, int index)
		{
			return BulkResponseCommand(RedisConstants.LIndex, ConvertToByteArray(key), ConvertToByteArray(index));
		}

		public Task<long> LInsertAsync<TPivot,TValue>(string key, TPivot pivot, TValue value, bool before = true)
		{
			return LInsertAsync(key, Serialize(pivot), Serialize(value), before);
		}

		public Task<long> LInsertAsync(string key, byte[] pivot, byte[] value, bool before = true)
		{
			return IntegerResponseCommand(RedisConstants.LInsert,
				ConvertToByteArray(key),before?RedisConstants.Before:RedisConstants.After,
				pivot, value);
		}

		public Task<long> LLenAsync(string key)
		{
			return IntegerResponseCommand(RedisConstants.LLen, ConvertToByteArray(key));
		}

		public Task<long> LPushXAsync<T>(string key, T value)
		{
			return LPushXAsync(key, Serialize(value));
		}

		public Task<long> LPushXAsync(string key, byte[] value)
		{
			return IntegerResponseCommand(RedisConstants.LPushX, ConvertToByteArray(key), value);
		}

		public Task<MultiBulk> LRangeAsync(string key, int start, int stop)
		{
			return MultiBulkResponseCommand(RedisConstants.LRange,
				ConvertToByteArray(key), ConvertToByteArray(start), ConvertToByteArray(stop));
		}

		public Task<long> LRemAsync<T>(string key, int count, T value)
		{
			return LRemAsync(key, count, Serialize(value));
		}

		public Task<long> LRemAsync(string key, int count, byte[] value)
		{
			return IntegerResponseCommand(RedisConstants.LRem,
				ConvertToByteArray(key), ConvertToByteArray(count), value);
		}

		public Task<string> LSetAsync<T>(string key, int index, T value)
		{
			return LSetAsync(key, index, Serialize(value));
		}

		public Task<string> LSetAsync(string key, int index, byte[] value)
		{
			return StatusResponseCommand(RedisConstants.LSet,
				ConvertToByteArray(key), ConvertToByteArray(index), value);
		}

		public Task<string> LTrimAsync(string key, int start, int stop)
		{
			return StatusResponseCommand(RedisConstants.LTrim,
				ConvertToByteArray(key), ConvertToByteArray(start), ConvertToByteArray(stop));
		}

		public Task<long> RPushXAsync<T>(string key, T values)
		{
			return RPushXAsync(key, Serialize(values));
		}

		public Task<long> RPushXAsync(string key, byte[] values)
		{
			return IntegerResponseCommand(RedisConstants.RPushX, ConvertToByteArray(key), values);
		}
	}
}
