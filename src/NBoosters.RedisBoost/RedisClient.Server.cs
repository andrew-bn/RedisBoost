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
		public Task<string> FlushDbAsync()
		{
			return StatusResponseCommand(RedisConstants.FlushDb);
		}
		public Task<string> FlushAllAsync()
		{
			return StatusResponseCommand(RedisConstants.FlushAll);
		}
		public Task<string> BgRewriteAofAsync()
		{
			return StatusResponseCommand(RedisConstants.BgRewriteAof);
		}
		public Task<string> BgSaveAsync()
		{
			return StatusResponseCommand(RedisConstants.BgSave);
		}
		public Task<Bulk> ClientListAsync()
		{
			return BulkResponseCommand(RedisConstants.Client,RedisConstants.List);
		}
		internal Task<string> ClientSetNameAsync(byte[] connectionName)
		{
			return StatusResponseCommand(RedisConstants.Client, RedisConstants.SetName, connectionName);
		}
		internal Task<Bulk> ClientGetNameAsync()
		{
			return BulkResponseCommand(RedisConstants.Client, RedisConstants.GetName);
		}
		public Task<long> DbSizeAsync()
		{
			return IntegerResponseCommand(RedisConstants.DbSize);
		}
		public Task<RedisResponse> ConfigGetAsync(string parameter)
		{
			return ExecutePipelinedCommand(RedisConstants.Config,RedisConstants.Get,ConvertToByteArray(parameter));
		}

		public Task<string> ConfigSetAsync<T>(string parameter, T value)
		{
			return ConfigSetAsync(parameter, Serialize(value));
		}

		public Task<string> ConfigSetAsync(string parameter,byte[] value)
		{
			return StatusResponseCommand(RedisConstants.Config, RedisConstants.Set, ConvertToByteArray(parameter), value);
		}
		public Task<string> ConfigResetStatAsync()
		{
			return StatusResponseCommand(RedisConstants.Config, RedisConstants.ResetStat);
		}
		public Task<string> ClientKillAsync(string ip, int port)
		{
			return StatusResponseCommand(RedisConstants.Client, RedisConstants.Kill,
				ConvertToByteArray(string.Format("{0}:{1}",ip,port)));
		}
		public Task<Bulk> InfoAsync()
		{
			return InfoAsync(null);
		}

		public Task<Bulk> InfoAsync(string section)
		{
			return section == null
					? BulkResponseCommand(RedisConstants.Info)
					: BulkResponseCommand(RedisConstants.Info, ConvertToByteArray(section));
		}
		public Task<long> LastSaveAsync()
		{
			return IntegerResponseCommand(RedisConstants.LastSave);
		}
		public Task<string> SaveAsync()
		{
			return StatusResponseCommand(RedisConstants.Save);
		}
		public Task<string> ShutDownAsync()
		{
			return StatusResponseCommand(RedisConstants.ShutDown);
		}
		public Task<string> ShutDownAsync(bool save)
		{
			return StatusResponseCommand(RedisConstants.ShutDown,save?RedisConstants.Save:RedisConstants.NoSave);
		}
		public Task<string> SlaveOfAsync(string host,int port)
		{
			return StatusResponseCommand(RedisConstants.SlaveOf,ConvertToByteArray(host), ConvertToByteArray(port));
		}
		public Task<MultiBulk> TimeAsync()
		{
			return MultiBulkResponseCommand(RedisConstants.Time);
		}
	}
}
