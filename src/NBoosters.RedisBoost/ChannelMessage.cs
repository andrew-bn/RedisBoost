namespace NBoosters.RedisBoost
{
	public struct ChannelMessage
	{
		public readonly ChannelMessageType MessageType;
		public readonly string[] Channels;
		public readonly RedisResponse Value;

		internal ChannelMessage(ChannelMessageType messageType, RedisResponse value, params string[] channels)
		{
			MessageType = messageType;
			Value = value;
			Channels = channels;
		}
	}
}
