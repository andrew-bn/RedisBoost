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
		public Task<string> AuthAsync(string password)
		{
			return StatusCommand(RedisConstants.Auth, password.ToBytes());
		}

		public Task<Bulk> EchoAsync<T>(T message)
		{
			return EchoAsync(Serialize(message));
		}

		public Task<Bulk> EchoAsync(byte[] message)
		{
			return BulkCommand(RedisConstants.Echo, message);
		}

		public Task<string> PingAsync()
		{
			return StatusCommand(RedisConstants.Ping);
		}

		public Task<string> QuitAsync()
		{
			SetQuitState();
			return StatusCommand(RedisConstants.Quit);
		}

		public Task<string> SelectAsync(int index)
		{
			return StatusCommand(RedisConstants.Select, index.ToBytes());
		}
	}
}
