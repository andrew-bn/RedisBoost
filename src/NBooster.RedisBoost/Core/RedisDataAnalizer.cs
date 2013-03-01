using System.Text;

namespace NBooster.RedisBoost.Core
{
	internal class RedisDataAnalizer : IRedisDataAnalizer
	{
		public int ConvertToInt(string data)
		{
			return int.Parse(data);
		}
		public long ConvertToLong(string data)
		{
			return long.Parse(data);
		}
		public bool IsErrorReply(byte firstByte)
		{
			return firstByte == RedisConstants.Minus;
		}
		public bool IsBulkReply(byte firstByte)
		{
			return firstByte == RedisConstants.Dollar;
		}
		public bool IsMultiBulkReply(byte firstByte)
		{
			return firstByte == RedisConstants.Asterix;
		}
		public bool IsIntReply(byte firstByte)
		{
			return firstByte == RedisConstants.Colon;
		}
		public bool IsStatusReply(byte firstByte)
		{
			return firstByte == RedisConstants.Plus;
		}
		
		public string ConvertToString(byte[] line, int startIndex)
		{
			return Encoding.UTF8.GetString(line, startIndex, line.Length - startIndex);
		}

		public byte[] ConvertToByteArray(string value)
		{
			return Encoding.UTF8.GetBytes(value);
		}

		public byte[] ConvertToByteArray(int value)
		{
			return ConvertToByteArray(value.ToString());
		}
		public byte[] ConvertToByteArray(long value)
		{
			return ConvertToByteArray(value.ToString());
		}
	}
}
