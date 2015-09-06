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
			var request = ComposeRequest(RedisConstants.SAdd, key.ToBytes(), members);
			return IntegerCommand(request);
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
			var request = ComposeRequest(RedisConstants.SRem, key.ToBytes(), members);
			return IntegerCommand(request);
		}

		public Task<long> SCardAsync(string key)
		{
			return IntegerCommand(RedisConstants.SCard, key.ToBytes());
		}

		public Task<MultiBulk> SDiffAsync(params string[] keys)
		{
			var request = ComposeRequest(RedisConstants.SDiff, keys);
			return MultiBulkCommand(request);
		}

		public Task<long> SDiffStoreAsync(string destinationKey, params string[] keys)
		{
			var request = ComposeRequest(RedisConstants.SDiffStore, destinationKey.ToBytes(), keys);
			return IntegerCommand(request);
		}

		public Task<MultiBulk> SUnionAsync(params string[] keys)
		{
			var request = ComposeRequest(RedisConstants.SUnion, keys);
			return MultiBulkCommand(request);
		}

		public Task<long> SUnionStoreAsync(string destinationKey, params string[] keys)
		{
			var request = ComposeRequest(RedisConstants.SUnionStore, destinationKey.ToBytes(), keys);
			return IntegerCommand(request);
		}

		public Task<MultiBulk> SInterAsync(params string[] keys)
		{
			var request = ComposeRequest(RedisConstants.SInter, keys);
			return MultiBulkCommand(request);
		}

		public Task<long> SInterStoreAsync(string destinationKey, params string[] keys)
		{
			var request = ComposeRequest(RedisConstants.SInterStore, destinationKey.ToBytes(), keys);
			return IntegerCommand(request);
		}

		public Task<long> SIsMemberAsync<T>(string key, T value)
		{
			return SIsMemberAsync(key, Serialize(value));
		}

		public Task<long> SIsMemberAsync(string key, byte[] value)
		{
			return IntegerCommand(RedisConstants.SIsMember, key.ToBytes(), value);
		}

		public Task<MultiBulk> SMembersAsync(string key)
		{
			return MultiBulkCommand(RedisConstants.SMembers, key.ToBytes());
		}

		public Task<long> SMoveAsync<T>(string sourceKey, string destinationKey, T member)
		{
			return SMoveAsync(sourceKey, destinationKey, Serialize(member));
		}

		public Task<long> SMoveAsync(string sourceKey, string destinationKey, byte[] member)
		{
			return IntegerCommand(RedisConstants.SMove, sourceKey.ToBytes(),
				destinationKey.ToBytes(), member);
		}

		public Task<Bulk> SPopAsync(string key)
		{
			return BulkCommand(RedisConstants.SPop, key.ToBytes());
		}

		public Task<Bulk> SRandMemberAsync(string key)
		{
			return BulkCommand(RedisConstants.SRandMember, key.ToBytes());
		}

		public Task<MultiBulk> SRandMemberAsync(string key, int count)
		{
			return MultiBulkCommand(RedisConstants.SRandMember, key.ToBytes(),
				count.ToBytes());
		}
	}
}
