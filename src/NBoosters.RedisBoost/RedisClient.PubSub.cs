using System;
using System.Threading.Tasks;
using NBoosters.RedisBoost.Core;

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
		private async Task<IRedisSubscription> SubscriptionCommandAsync(byte[] commandName, string[] channels)
		{
			_state = ClientState.Subscription;

			var request = new byte[1 + channels.Length][];
			request[0] = commandName;

			for (int i = 0; i < channels.Length; i++)
				request[1 + i] = ConvertToByteArray(channels[i]);

			await SendDirectReqeust(request).ConfigureAwait(false);
			return this;
		}

		public Task<ChannelMessage> ReadMessageAsync()
		{
			return ReadMessageAsync(ChannelMessageType.Any);
		}

		public async Task<ChannelMessage> ReadMessageAsync(ChannelMessageType messageTypeFilter)
		{
			ChannelMessageType messageType;
			MultiBulk response;
			do
			{
				var reply = await ReadDirectResponse().ConfigureAwait(false);
				
				if (reply.ResponseType == RedisResponseType.Status && _state != ClientState.Connect)
					return new ChannelMessage(ChannelMessageType.Quit, null, null);

				if (reply.ResponseType != RedisResponseType.MultiBulk)
					throw new RedisException("Invalid channel response. Expected MultiBulk, but was " + reply.ResponseType);
				
				response = reply.AsMultiBulk();
				
				var messageTypeName = ConvertToString(response[0].AsBulk());

				if (!Enum.TryParse(messageTypeName, true, out messageType))
					messageType = ChannelMessageType.Any;

			} while (!messageTypeFilter.HasFlag(messageType) &&
					 !messageTypeFilter.HasFlag(ChannelMessageType.Any));

			var channels = new string[response.Length - 2];

			for (var i = 1; i < (response.Length - 1); i++)
				channels[i - 1] = ConvertToString(response[i].AsBulk());

			var lastReply = response[response.Length - 1];

			return new ChannelMessage(messageType, lastReply, channels);
		}
		Task IRedisSubscription.QuitAsync()
		{
			_state = ClientState.Quit;
			return SendDirectReqeust(RedisConstants.Quit);
		}
	}
}
