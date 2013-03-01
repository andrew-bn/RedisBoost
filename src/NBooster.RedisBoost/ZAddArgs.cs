namespace NBooster.RedisBoost
{
	public struct ZAddArgs
	{
		internal byte[] Member;
		internal long IntScore;
		internal double DoubleScore;
		internal bool UseIntValue;

		public ZAddArgs(long score, byte[] member)
		{
			UseIntValue = true;
			IntScore = score;
			DoubleScore = 0;
			Member = member;
		}

		public ZAddArgs(double score, byte[] member)
		{
			UseIntValue = false;
			IntScore = 0;
			DoubleScore = score;
			Member = member;
		}
	}
}
