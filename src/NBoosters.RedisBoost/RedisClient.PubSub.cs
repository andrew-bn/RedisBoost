using System;
using System.Threading.Tasks;
using NBoosters.RedisBoost.Core;
using NBoosters.RedisBoost.Core.Misk;

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

			var request = new byte[1 + channels.Length][];
			request[0] = commandName;

			for (int i = 0; i < channels.Length; i++)
				request[1 + i] = ConvertToByteArray(channels[i]);

			return SendDirectReqeust(request).ContinueWithIfNoError(t => (IRedisSubscription)this);
		}

		public Task<ChannelMessage> ReadMessageAsync()
		{
			return ReadMessageAsync(ChannelMessageType.Any);
		}

		public  Task<ChannelMessage> ReadMessageAsync(ChannelMessageType messageTypeFilter)
		{
			var tcs = new TaskCompletionSource<ChannelMessage>();
			ReadDirectResponse().ContinueWithIfNoError(t => ReadMessageContinuation(t, messageTypeFilter, tcs));
			return tcs.Task;
		}
		private void ReadMessageContinuation(Task<RedisResponse> readTask, ChannelMessageType messageTypeFilter, TaskCompletionSource<ChannelMessage> tcs )
		{
			if (readTask.IsFaulted)
			{
				tcs.SetException(readTask.Exception.UnwrapAggregation());
				return;
			}

			ChannelMessageType messageType;
			MultiBulk response;

			var reply = readTask.Result;

			if (reply.ResponseType == RedisResponseType.Status && _state != ClientState.Connect)
			{
				tcs.SetResult(new ChannelMessage(ChannelMessageType.Quit, null, null));
				return;
			}

			if (reply.ResponseType != RedisResponseType.MultiBulk)
			{
				tcs.SetException(new RedisException("Invalid channel response. Expected MultiBulk, but was " + reply.ResponseType));
				return;
			}
			response = reply.AsMultiBulk();

			var messageTypeName = ConvertToString(response[0].AsBulk());

			if (!Enum.TryParse(messageTypeName, true, out messageType))
				messageType = ChannelMessageType.Any;

			if (!messageTypeFilter.HasFlag(messageType) &&
			    !messageTypeFilter.HasFlag(ChannelMessageType.Any))
			{
				ReadDirectResponse().ContinueWithIfNoError(t => ReadMessageContinuation(t, messageTypeFilter, tcs));
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
			return SendDirectReqeust(RedisConstants.Quit);
		}
	}
}
