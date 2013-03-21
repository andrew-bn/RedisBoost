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
using NBoosters.RedisBoost.Core;

namespace NBoosters.RedisBoost
{
	public partial class RedisClient
	{
		public Task<long> SAddAsync<T>(string key, params T[] members)
		{
			return SAddAsync(key, Serialize(members));
		}

		public Task<long> SAddAsync(string key, params object[] members)
		{
			return SAddAsync(key, Serialize(members));
		}

		public Task<long> SAddAsync(string key, params byte[][] members)
		{
			var request = ComposeRequest(RedisConstants.SAdd, ConvertToByteArray(key), members);
			return IntegerResponseCommand(request);
		}

		public Task<long> SRemAsync<T>(string key, params T[] members)
		{
			return SRemAsync(key, Serialize(members));
		}

		public Task<long> SRemAsync(string key, params object[] members)
		{
			return SRemAsync(key, Serialize(members));
		}

		public Task<long> SRemAsync(string key, params byte[][] members)
		{
			var request = ComposeRequest(RedisConstants.SRem, ConvertToByteArray(key), members);
			return IntegerResponseCommand(request);
		}

		public Task<long> SCardAsync(string key)
		{
			return IntegerResponseCommand(RedisConstants.SCard, ConvertToByteArray(key));
		}

		public Task<MultiBulk> SDiffAsync(params string[] keys)
		{
			var request = ComposeRequest(RedisConstants.SDiff, keys);
			return MultiBulkResponseCommand(request);
		}

		public Task<long> SDiffStoreAsync(string destinationKey, params string[] keys)
		{
			var request = ComposeRequest(RedisConstants.SDiffStore, ConvertToByteArray(destinationKey), keys);
			return IntegerResponseCommand(request);
		}

		public Task<MultiBulk> SUnionAsync(params string[] keys)
		{
			var request = ComposeRequest(RedisConstants.SUnion, keys);
			return MultiBulkResponseCommand(request);
		}

		public Task<long> SUnionStoreAsync(string destinationKey, params string[] keys)
		{
			var request = ComposeRequest(RedisConstants.SUnionStore, ConvertToByteArray(destinationKey), keys);
			return IntegerResponseCommand(request);
		}

		public Task<MultiBulk> SInterAsync(params string[] keys)
		{
			var request = ComposeRequest(RedisConstants.SInter, keys);
			return MultiBulkResponseCommand(request);
		}

		public Task<long> SInterStoreAsync(string destinationKey, params string[] keys)
		{
			var request = ComposeRequest(RedisConstants.SInterStore, ConvertToByteArray(destinationKey), keys);
			return IntegerResponseCommand(request);
		}

		public Task<long> SIsMemberAsync<T>(string key, T value)
		{
			return SIsMemberAsync(key, Serialize(value));
		}

		public Task<long> SIsMemberAsync(string key, byte[] value)
		{
			return IntegerResponseCommand(RedisConstants.SIsMember, ConvertToByteArray(key), value);
		}

		public Task<MultiBulk> SMembersAsync(string key)
		{
			return MultiBulkResponseCommand(RedisConstants.SMembers, ConvertToByteArray(key));
		}

		public Task<long> SMoveAsync<T>(string sourceKey, string destinationKey, T member)
		{
			return SMoveAsync(sourceKey, destinationKey, Serialize(member));
		}

		public Task<long> SMoveAsync(string sourceKey, string destinationKey, byte[] member)
		{
			return IntegerResponseCommand(RedisConstants.SMove, ConvertToByteArray(sourceKey),
				ConvertToByteArray(destinationKey), member);
		}

		public Task<Bulk> SPopAsync(string key)
		{
			return BulkResponseCommand(RedisConstants.SPop, ConvertToByteArray(key));
		}

		public Task<Bulk> SRandMemberAsync(string key)
		{
			return BulkResponseCommand(RedisConstants.SRandMember, ConvertToByteArray(key));
		}

		public Task<MultiBulk> SRandMemberAsync(string key, int count)
		{
			return MultiBulkResponseCommand(RedisConstants.SRandMember, ConvertToByteArray(key),
				ConvertToByteArray(count));
		}
	}
}
