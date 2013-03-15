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
		public Task<string> DiscardAsync()
		{
			return StatusResponseCommand(RedisConstants.Discard);
		}
		public Task<MultiBulk> ExecAsync()
		{
			return MultiBulkResponseCommand(RedisConstants.Exec);
		}
		public Task<string> MultiAsync()
		{
			return StatusResponseCommand(RedisConstants.Multi);
		}
		public Task<string> UnwatchAsync()
		{
			return StatusResponseCommand(RedisConstants.Unwatch);
		}
		public Task<string> WatchAsync(params string[] keys)
		{
			if (keys.Length == 0)
				throw new ArgumentException("Invalid keys count", "keys");

			var request = new byte[keys.Length + 1][];
			request[0] = RedisConstants.Watch;
			for (int i = 0; i < keys.Length; i++)
				request[i + 1] = ConvertToByteArray(keys[i]);

			return StatusResponseCommand(request);
		}
	}
}
