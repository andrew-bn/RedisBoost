using NBoosters.RedisBoost.Core.Serialization;

namespace NBoosters.RedisBoost
{
	public class Bulk : RedisResponse
	{
		public byte[] Value { get; private set; }

		internal Bulk(byte[] value, IRedisSerializer serializer)
			: base(RedisResponseType.Bulk, serializer)
		{
			Value = value;
		}
		public int Length
		{
			get { return Value==null?0:Value.Length; }
		}
		public override string ToString()
		{
			return Value.ToString();
		}
	}
}