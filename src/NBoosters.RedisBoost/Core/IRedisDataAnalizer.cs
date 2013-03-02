namespace NBoosters.RedisBoost.Core
{
	internal interface IRedisDataAnalizer
	{
		int ConvertToInt(string data);
		long ConvertToLong(string data);
		bool IsErrorReply(byte firstByte);

		string ConvertToString(byte[] line, int startIndex);

		byte[] ConvertToByteArray(string value);

		byte[] ConvertToByteArray(int value);
		byte[] ConvertToByteArray(long value);

		bool IsBulkReply(byte firstByte);

		bool IsIntReply(byte firstByte);

		bool IsStatusReply(byte firstByte);

		bool IsMultiBulkReply(byte firstByte);
	}
}
