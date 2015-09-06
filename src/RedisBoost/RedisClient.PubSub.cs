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
		public Task<MultiBulk> PubSubChannels(string pattern)
		{
			return MultiBulkCommand(RedisConstants.PubSub, RedisConstants.Channels, pattern.ToBytes());
		}

		public Task<MultiBulk> PubSubNumSub(params string[] channels)
		{
			var request = ComposeRequest(RedisConstants.PubSub, RedisConstants.NumSub, channels);
			return MultiBulkCommand(request);
		}

		public Task<long> PubSubNumPat()
		{
			return IntegerCommand(RedisConstants.PubSub, RedisConstants.NumSub);
		}

		public Task<long> PublishAsync<T>(string channel, T message)
		{
			return PublishAsync(channel, Serialize(message));
		}

		public Task<long> PublishAsync(string channel, byte[] message)
		{
			return IntegerCommand(RedisConstants.Publish, channel.ToBytes(), message);
		}

		public Task<IRedisSubscription> SubscribeAsync(params string[] channels)
		{
			OneWayMode();
			return SubscriptionCommandAsync(RedisConstants.Subscribe, channels);
		}

		public Task<IRedisSubscription> PSubscribeAsync(params string[] pattern)
		{
			OneWayMode();
			return SubscriptionCommandAsync(RedisConstants.PSubscribe, pattern);
		}

		Task IRedisSubscription.SubscribeAsync(params string[] channels)
		{
			return SubscriptionCommandAsync(RedisConstants.Subscribe, channels);
		}

		Task IRedisSubscription.PSubscribeAsync(params string[] pattern)
		{
			return SubscriptionCommandAsync(RedisConstants.PSubscribe, pattern);
		}

		public Task UnsubscribeAsync(params string[] channels)
		{
			return SubscriptionCommandAsync(RedisConstants.Unsubscribe, channels);
		}

		public Task PUnsubscribeAsync(params string[] channels)
		{
			return SubscriptionCommandAsync(RedisConstants.PUnsubscribe, channels);
		}

		private Task<IRedisSubscription> SubscriptionCommandAsync(byte[] commandName, string[] channels)
		{
			SetQuitState();
			var request = ComposeRequest(commandName, channels);
			return SendDirectRequest(request).ContinueWithIfNoError(t => (IRedisSubscription)this);
		}

		public Task<ChannelMessage> ReadMessageAsync()
		{
			return TryReadMessageAsync(null);
		}

		public Task<ChannelMessage> ReadMessageAsync(ChannelMessageType messageTypeFilter)
		{
			return TryReadMessageAsync(messageTypeFilter)
				.ContinueWithIfNoError(t =>
					{
						if (t.Result.MessageType == ChannelMessageType.Unknown)
							throw new RedisException("Unexpected type of message received");
						return t.Result;
					});
		}

		public Task<ChannelMessage> TryReadMessageAsync(ChannelMessageType? messageTypeFilter)
		{
			var tcs = new TaskCompletionSource<ChannelMessage>();
			ReadDirectResponse().ContinueWith(t => ReadMessageContinuation(t, messageTypeFilter, tcs));
			return tcs.Task;
		}

		private void ReadMessageContinuation(Task<RedisResponse> readTask, ChannelMessageType? messageTypeFilter, TaskCompletionSource<ChannelMessage> tcs)
		{
			if (readTask.IsFaulted)
			{
				tcs.SetException(readTask.Exception.UnwrapAggregation());
				return;
			}

			if (readTask.Result.ResponseType != ResponseType.MultiBulk)
			{
				tcs.SetResult(new ChannelMessage(ChannelMessageType.Unknown, readTask.Result, new string[0]));
				return;
			}
			
			ChannelMessageType messageType;
			var response = readTask.Result.AsMultiBulk();
			if (response.Length < 3 || //1 - message type, 2.. - channel, ..3 - message
				response[0].ResponseType != ResponseType.Bulk ||
				!Enum.TryParse(((byte[])response[0].AsBulk()).AsString(), true, out messageType)) 
			{
				tcs.SetResult(new ChannelMessage(ChannelMessageType.Unknown, readTask.Result, new string[0]));
				return;
			}

			if (messageTypeFilter.HasValue && !messageTypeFilter.Value.HasFlag(messageType))
			{
				ReadDirectResponse().ContinueWith(t => ReadMessageContinuation(t, messageTypeFilter, tcs));
				return;
			}

			var channels = new string[response.Length - 2];

			for (var i = 1; i < (response.Length - 1); i++)
				channels[i - 1] = ((byte[])response[i].AsBulk()).AsString();

			var lastReply = response[response.Length - 1];

			tcs.SetResult(new ChannelMessage(messageType, lastReply, channels));
		}

		Task IRedisSubscription.QuitAsync()
		{
			SetQuitState();
			return SendDirectRequest(RedisConstants.Quit);
		}
	}
}
