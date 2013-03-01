namespace NBooster.RedisBoost
{
	public struct MSetArgs
	{
		private string _keyOrField;
		internal string KeyOrField { get { return _keyOrField; } }
		private byte[] _value;
		internal byte[] Value { get { return _value; } }

		public MSetArgs(string keyOrField, byte[] value)
		{
			_keyOrField = keyOrField;
			_value = value;
		}
	}
}
