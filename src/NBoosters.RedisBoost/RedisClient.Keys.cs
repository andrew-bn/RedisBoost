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
using NBoosters.RedisBoost.Core.Misk;

namespace NBoosters.RedisBoost
{
	public partial class RedisClient
	{
		public Task<MultiBulk> KeysAsync(string pattern)
		{
			return MultiBulkResponseCommand(RedisConstants.Keys, ConvertToByteArray(pattern));
		}

		public Task<long> DelAsync(string key)
		{
			return IntegerResponseCommand(RedisConstants.Del, ConvertToByteArray(key));
		}

		public Task<string> MigrateAsync(string host,int port, string key, int destinationDb, int timeout)
		{
			return StatusResponseCommand(RedisConstants.Migrate, 
										 ConvertToByteArray(host),ConvertToByteArray(port),
										 ConvertToByteArray(key), ConvertToByteArray(destinationDb),
										 ConvertToByteArray(timeout));
		}

		public Task<Bulk> DumpAsync(string key)
		{
			return BulkResponseCommand(RedisConstants.Dump, ConvertToByteArray(key));
		}

		public Task<string> RestoreAsync(string key, int ttlInMilliseconds, byte[] serializedValue)
		{
			return StatusResponseCommand(RedisConstants.Restore, ConvertToByteArray(key),
				ConvertToByteArray(ttlInMilliseconds), serializedValue);
		}

		public Task<long> ExistsAsync(string key)
		{
			return IntegerResponseCommand(RedisConstants.Exists, ConvertToByteArray(key));
		}

		public Task<long> ExpireAsync(string key, int seconds)
		{
			return IntegerResponseCommand(RedisConstants.Expire, ConvertToByteArray(key), ConvertToByteArray(seconds));
		}

		public Task<long> PExpireAsync(string key, int milliseconds)
		{
			return IntegerResponseCommand(RedisConstants.PExpire, ConvertToByteArray(key), ConvertToByteArray(milliseconds));
		}

		public Task<long> ExpireAtAsync(string key, DateTime timestamp)
		{
			var seconds = (int)(timestamp - RedisConstants.InitialUnixTime).TotalSeconds;
			return IntegerResponseCommand(RedisConstants.ExpireAt, ConvertToByteArray(key), 
				ConvertToByteArray(seconds));
		}

		public Task<long> PersistAsync(string key)
		{
			return IntegerResponseCommand(RedisConstants.Persist, ConvertToByteArray(key));
		}

		public Task<long> PttlAsync(string key)
		{
			return IntegerResponseCommand(RedisConstants.Pttl, ConvertToByteArray(key));
		}

		public Task<long> TtlAsync(string key)
		{
			return IntegerResponseCommand(RedisConstants.Ttl, ConvertToByteArray(key));
		}

		public Task<string> TypeAsync(string key)
		{
			return StatusResponseCommand(RedisConstants.Type, ConvertToByteArray(key));
		}

		public Task<string> RandomKeyAsync()
		{
			return BulkResponseCommand(RedisConstants.RandomKey)
				.ContinueWithIfNoError(t =>
					{
						var result = t.Result;
						return (result == null || result.IsNull)
								? String.Empty
								: ConvertToString(result);
					});
		}

		public Task<string> RenameAsync(string key, string newKey)
		{
			return StatusResponseCommand(RedisConstants.Rename, ConvertToByteArray(key), ConvertToByteArray(newKey));
		}

		public Task<long> RenameNxAsync(string key, string newKey)
		{
			return IntegerResponseCommand(RedisConstants.RenameNx, ConvertToByteArray(key), ConvertToByteArray(newKey));
		}

		public Task<long> MoveAsync(string key, int db)
		{
			return IntegerResponseCommand(RedisConstants.Move, ConvertToByteArray(key), ConvertToByteArray(db));
		}

		public Task<RedisResponse> ObjectAsync(Subcommand subcommand, params string[] args)
		{
			var request = ComposeRequest(RedisConstants.Object, ConvertToByteArray(subcommand), args);
			return ExecutePipelinedCommand(request);
		}

		public Task<RedisResponse> SortAsync(string key, string by = null, long? limitOffset = null,
								 long? limitCount = null, bool? asc = null, bool alpha = false, string destination = null,
								 string[] getPatterns = null)
		{
			var request = ComposeRequest(key, by, limitOffset, limitCount, asc, alpha, destination, getPatterns);
			return ExecutePipelinedCommand(request);
		}

		#region request composing
		private byte[][] ComposeRequest(string key, string by, long? limitOffset, long? limitCount, bool? asc, bool alpha,
		                                string destination, string[] getPatterns)
		{
			var paramsCount = 2;
			paramsCount += by != null ? 2 : 0;
			paramsCount += limitOffset.HasValue && limitCount.HasValue ? 3 : 0;
			paramsCount += asc.HasValue ? 1 : 0;
			paramsCount += alpha ? 1 : 0;
			paramsCount += destination != null ? 2 : 0;
			paramsCount += getPatterns != null ? getPatterns.Length*2 : 0;

			int index = -1;
			var request = new byte[paramsCount][];
			request[++index] = RedisConstants.Sort;
			request[++index] = ConvertToByteArray(key);
			if (by != null)
			{
				request[++index] = RedisConstants.By;
				request[++index] = ConvertToByteArray(by);
			}
			if (limitOffset.HasValue)
			{
				request[++index] = RedisConstants.Limit;
				request[++index] = ConvertToByteArray(limitOffset.Value);
				request[++index] = ConvertToByteArray(limitCount.Value);
			}
			if (getPatterns != null)
			{
				for (int i = 0; i < getPatterns.Length; i++)
				{
					request[++index] = RedisConstants.Get;
					request[++index] = ConvertToByteArray(getPatterns[i]);
				}
			}
			if (asc.HasValue)
				request[++index] = asc.Value ? RedisConstants.Asc : RedisConstants.Desc;
			if (alpha)
				request[++index] = RedisConstants.Alpha;
			if (destination != null)
			{
				request[++index] = RedisConstants.Store;
				request[++index] = ConvertToByteArray(destination);
			}
			return request;
		}
		#endregion
	}
}
