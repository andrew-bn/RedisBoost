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

namespace RedisBoost
{
	public interface IRedisSubscription : IDisposable
	{
		/// <summary>
		/// Listen for messages published to the given channels.
		/// <br/> Complexity: O(N) where N is the number of channels to subscribe to.
		/// </summary>
		/// <param name="channels"></param>
		/// <returns></returns>
		Task SubscribeAsync(params string[] channels);
		/// <summary>
		/// Listen for messages published to channels matching the given patterns.
		/// <br/> Complexity: O(N) where N is the number of patterns the client is already subscribed to.
		/// </summary>
		/// <param name="pattern"></param>
		/// <returns></returns>
		Task PSubscribeAsync(params string[] pattern);
		/// <summary>
		/// Stop listening for messages posted to the given channels.
		/// <br/> Complexity: O(N) where N is the number of clients already subscribed to a channel.
		/// </summary>
		/// <param name="channels"></param>
		/// <returns></returns>
		Task UnsubscribeAsync(params string[] channels);
		/// <summary>
		/// Stop listening for messages posted to channels matching the given patterns.
		/// <br/> Complexity: O(N+M) where N is the number of patterns the client is already subscribed and M is the number of total patterns subscribed in the system (by any client).
		/// </summary>
		/// <param name="channels"></param>
		/// <returns></returns>
		Task PUnsubscribeAsync(params string[] channels);
		/// <summary>
		/// Read message from channel
		/// </summary>
		/// <returns></returns>
		Task<ChannelMessage> ReadMessageAsync();
		/// <summary>
		/// Read and return first message from channel that fits filter
		/// </summary>
		/// <param name="messageTypeFilter"></param>
		/// <returns></returns>
		Task<ChannelMessage> ReadMessageAsync(ChannelMessageType messageTypeFilter);
		/// <summary>
		/// Close the connection
		/// </summary>
		/// <returns></returns>
		Task QuitAsync();
		/// <summary>
		/// Closes socket connection with Redis
		/// </summary>
		/// <returns></returns>
		Task DisconnectAsync();
	}
}
