namespace NBoosters.RedisBoost
{
	public struct MSetArgs
	{
		internal readonly string KeyOrField;
		internal readonly byte[] Value;

		public MSetArgs(string keyOrField, byte[] value)
		{
			KeyOrField = keyOrField;
			Value = value;
		}
	}
}
