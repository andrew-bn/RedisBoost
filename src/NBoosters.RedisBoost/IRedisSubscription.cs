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

namespace NBoosters.RedisBoost
{
	public interface IRedisSubscription : IDisposable
	{
		Task SubscribeAsync(params string[] channels);
		Task PSubscribeAsync(params string[] pattern);
		Task UnsubscribeAsync(params string[] channels);
		Task PUnsubscribeAsync(params string[] channels);

		Task<ChannelMessage> ReadMessageAsync();
		Task<ChannelMessage> ReadMessageAsync(ChannelMessageType messageTypeFilter);

		Task QuitAsync();
		Task DisconnectAsync();
	}
}
