namespace NBoosters.RedisBoost
{
	public struct ZAggrStoreArgs
	{
		internal long Weight;
		internal string Key;
		public ZAggrStoreArgs(string key, long weight)
		{
			Key = key;
			Weight = weight;
		}

	}
}
