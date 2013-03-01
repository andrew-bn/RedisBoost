namespace NBooster.RedisBoost
{
	public struct ChannelMessage
	{
		public readonly ChannelMessageType MessageType;
		public readonly string[] Channels;
		public readonly byte[] Value;

		internal ChannelMessage(ChannelMessageType messageType, byte[] value, params string[] channels)
		{
			MessageType = messageType;
			Value = value;
			Channels = channels;
		}
	}
}
