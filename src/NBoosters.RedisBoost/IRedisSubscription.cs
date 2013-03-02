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
