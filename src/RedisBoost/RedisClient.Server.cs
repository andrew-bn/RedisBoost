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
		public Task<string> FlushDbAsync()
		{
			return StatusCommand(RedisConstants.FlushDb);
		}

		public Task<string> FlushAllAsync()
		{
			return StatusCommand(RedisConstants.FlushAll);
		}

		public Task<string> BgRewriteAofAsync()
		{
			return StatusCommand(RedisConstants.BgRewriteAof);
		}

		public Task<string> BgSaveAsync()
		{
			return StatusCommand(RedisConstants.BgSave);
		}

		public Task<Bulk> ClientListAsync()
		{
			return BulkCommand(RedisConstants.Client,RedisConstants.List);
		}

		internal Task<string> ClientSetNameAsync(byte[] connectionName)
		{
			return StatusCommand(RedisConstants.Client, RedisConstants.SetName, connectionName);
		}

		internal Task<Bulk> ClientGetNameAsync()
		{
			return BulkCommand(RedisConstants.Client, RedisConstants.GetName);
		}

		public Task<long> DbSizeAsync()
		{
			return IntegerCommand(RedisConstants.DbSize);
		}

		public Task<RedisResponse> ConfigGetAsync(string parameter)
		{
			return ExecuteRedisCommand(RedisConstants.Config, RedisConstants.Get, parameter.ToBytes());
		}

		public Task<string> ConfigSetAsync<T>(string parameter, T value)
		{
			return ConfigSetAsync(parameter, Serialize(value));
		}

		public Task<string> ConfigSetAsync(string parameter,byte[] value)
		{
			return StatusCommand(RedisConstants.Config, RedisConstants.Set, parameter.ToBytes(), value);
		}

		public Task<string> ConfigResetStatAsync()
		{
			return StatusCommand(RedisConstants.Config, RedisConstants.ResetStat);
		}

		public Task<string> ClientKillAsync(string ip, int port)
		{
			return StatusCommand(RedisConstants.Client, RedisConstants.Kill, string.Format("{0}:{1}", ip, port).ToBytes());
		}

		public Task<Bulk> InfoAsync()
		{
			return InfoAsync(null);
		}

		public Task<Bulk> InfoAsync(string section)
		{
			return section == null
					? BulkCommand(RedisConstants.Info)
					: BulkCommand(RedisConstants.Info, section.ToBytes());
		}

		public Task<long> LastSaveAsync()
		{
			return IntegerCommand(RedisConstants.LastSave);
		}

		public Task<string> SaveAsync()
		{
			return StatusCommand(RedisConstants.Save);
		}

		public Task<string> ShutDownAsync()
		{
			return StatusCommand(RedisConstants.ShutDown);
		}

		public Task<string> ShutDownAsync(bool save)
		{
			return StatusCommand(RedisConstants.ShutDown,save?RedisConstants.Save:RedisConstants.NoSave);
		}

		public Task<string> SlaveOfAsync(string host,int port)
		{
			return StatusCommand(RedisConstants.SlaveOf, host.ToBytes(), port.ToBytes());
		}

		public Task<string> SlaveOfAsync()
		{
			return StatusCommand(RedisConstants.SlaveOf, RedisConstants.No, RedisConstants.One);
		}

		public Task<MultiBulk> TimeAsync()
		{
			return MultiBulkCommand(RedisConstants.Time);
		}
	}
}
