using System;
using System.Threading.Tasks;

namespace NBoosters.RedisBoost
{
	public interface IRedisSubscription : IDisposable
	{
		Task<IRedisSubscription> SubscribeAsync(params string[] channels);
		Task<IRedisSubscription> PSubscribeAsync(params string[] pattern);
		Task<IRedisSubscription> UnsubscribeAsync(params string[] channels);
		Task<IRedisSubscription> PUnsubscribeAsync(params string[] channels);

		Task<ChannelMessage> ReadMessageAsync();
		Task<ChannelMessage> ReadMessageAsync(ChannelMessageType messageTypeFilter);

		Task QuitAsync();
		Task DisconnectAsync();
	}
}
