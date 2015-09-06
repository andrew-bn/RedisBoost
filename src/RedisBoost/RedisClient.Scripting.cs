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
		public Task<RedisResponse> EvalAsync(string script, string[] keys, params object[] arguments)
		{
			return EvalAsync(script, keys, Serialize(arguments));
		}

		public Task<RedisResponse> EvalAsync(string script, string[] keys, params byte[][] arguments)
		{
			return Eval(RedisConstants.Eval, script.ToBytes(), keys, arguments);
		}

		public Task<RedisResponse> EvalShaAsync(byte[] sha1, string[] keys, params object[] arguments)
		{
			return EvalShaAsync(sha1, keys, Serialize(arguments));
		}

		public Task<RedisResponse> EvalShaAsync(byte[] sha1, string[] keys, params byte[][] arguments)
		{
			return Eval(RedisConstants.EvalSha, sha1, keys, arguments);
		}

		public Task<Bulk> ScriptLoadAsync(string script)
		{
			return BulkCommand(RedisConstants.Script, RedisConstants.Load, script.ToBytes());
		}

		public Task<MultiBulk> ScriptExistsAsync(params byte[][] sha1)
		{
			var request = ComposeRequest(RedisConstants.Script, RedisConstants.Exists, sha1);
			return MultiBulkCommand(request);
		}
		public Task<string> ScriptFlushAsync()
		{
			return StatusCommand(RedisConstants.Script, RedisConstants.Flush);
		}
		public Task<string> ScriptKillAsync()
		{
			return StatusCommand(RedisConstants.Script, RedisConstants.Kill);
		}
		private Task<RedisResponse> Eval(byte[] commandName, byte[] script, string[] keys, params byte[][] arguments)
		{
			var request = new byte[3 + keys.Length + arguments.Length][];
			request[0] = commandName;
			request[1] = script;
			request[2] = keys.Length.ToBytes();

			for (int i = 0; i < keys.Length; i++)
				request[3 + i] = keys[i].ToBytes();
			for (int i = 0; i < arguments.Length; i++)
				request[3 + keys.Length + i] = arguments[i];

			return ExecuteRedisCommand(request);
		}
	}
}
