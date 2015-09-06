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
using RedisBoost.Core;

namespace RedisBoost
{
	public partial class RedisClient
	{
		public Task<string> DiscardAsync()
		{
			return StatusCommand(RedisConstants.Discard);
		}

		public Task<MultiBulk> ExecAsync()
		{
			return MultiBulkCommand(RedisConstants.Exec);
		}

		public Task<string> MultiAsync()
		{
			return StatusCommand(RedisConstants.Multi);
		}

		public Task<string> UnwatchAsync()
		{
			return StatusCommand(RedisConstants.Unwatch);
		}

		public Task<string> WatchAsync(params string[] keys)
		{
			var request = ComposeRequest(RedisConstants.Watch, keys);
			return StatusCommand(request);
		}
	}
}
