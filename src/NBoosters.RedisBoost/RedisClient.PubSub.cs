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
using NBoosters.RedisBoost.Misk;

namespace NBoosters.RedisBoost
{
	public partial class RedisClient
	{
		public Task<long> PublishAsync<T>(string channel, T message)
		{
			return PublishAsync(channel, Serialize(message));
		}

		public Task<long> PublishAsync(string channel, byte[] message)
		{
			return IntegerResponseCommand(RedisConstants.Publish, ConvertToByteArray(channel), message);
		}

		public Task<IRedisSubscription> SubscribeAsync(params string[] channels)
		{
			ClosePipeline();
			return SubscriptionCommandAsync(RedisConstants.Subscribe, channels);
		}

		public Task<IRedisSubscription> PSubscribeAsync(params string[] pattern)
		{
			ClosePipeline();
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
			_state = ClientState.Subscription;
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
							throw new RedisException("Unexpected message received");
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
				tcs.SetResult(new ChannelMessage(ChannelMessageType.Unknown, 
					RedisResponse.CreateError(readTask.Exception.UnwrapAggregation().Message, Serializer), new string[0]));
				return;
			}
			if (readTask.Result.ResponseType != RedisResponseType.MultiBulk)
			{
				tcs.SetResult(new ChannelMessage(ChannelMessageType.Unknown, readTask.Result, new string[0]));
				return;
			}
			
			ChannelMessageType messageType;
			var response = readTask.Result.AsMultiBulk();
			if (response.Length < 3 || //1 - message type, 2.. - channel, ..3 - message
				response[0].ResponseType != RedisResponseType.Bulk ||
				!Enum.TryParse(ConvertToString(response[0].AsBulk()), true, out messageType)) 
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
				channels[i - 1] = ConvertToString(response[i].AsBulk());

			var lastReply = response[response.Length - 1];

			tcs.SetResult(new ChannelMessage(messageType, lastReply, channels));
		}

		Task IRedisSubscription.QuitAsync()
		{
			_state = ClientState.Quit;
			return SendDirectRequest(RedisConstants.Quit);
		}
	}
}
