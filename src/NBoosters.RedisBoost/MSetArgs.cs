namespace NBoosters.RedisBoost
{
	public struct MSetArgs
	{
		internal readonly string KeyOrField;
		internal readonly bool IsArray;
		internal readonly object Value;

		public MSetArgs(string keyOrField, byte[] value)
		{
			KeyOrField = keyOrField;
			Value = value;
			IsArray = true;
		}
		public MSetArgs(string keyOrField, object value)
		{
			KeyOrField = keyOrField;
			Value = value;
			IsArray = false;
		}
	}
}
