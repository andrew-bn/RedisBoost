namespace NBoosters.RedisBoost
{
	public struct ZAddArgs
	{
		internal object Member;
		internal bool IsArray;
		internal long IntScore;
		internal double DoubleScore;
		internal bool UseIntValue;

		public ZAddArgs(long score, byte[] member)
		{
			UseIntValue = true;
			IntScore = score;
			DoubleScore = 0;
			Member = member;
			IsArray = true;
		}

		public ZAddArgs(double score, byte[] member)
		{
			UseIntValue = false;
			IntScore = 0;
			DoubleScore = score;
			Member = member;
			IsArray = true;
		}
		public ZAddArgs(long score, object member)
		{
			UseIntValue = true;
			IntScore = score;
			DoubleScore = 0;
			Member = member;
			IsArray = false;
		}

		public ZAddArgs(double score, object member)
		{
			UseIntValue = false;
			IntScore = 0;
			DoubleScore = score;
			Member = member;
			IsArray = false;
		}
	}
}
