using System;

namespace NBooster.RedisBoost
{
	[Flags]
	public enum ChannelMessageType
	{
		Quit = 0,
		Any = 1,
		Subscribe = 2,
		Unsubscribe = 4,
		Message = 8,
		PMessage = 16,
		PSubscribe = 32,
		PUnsubscribe = 64,
	}
}
