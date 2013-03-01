using System;
using System.Runtime.Serialization;

namespace NBooster.RedisBoost
{
	[Serializable]
	public class RedisException:Exception
	{
		public RedisException(string message)
			: base(message)
		{
			
		}
		public RedisException(string message, Exception innerException)
			: base(message, innerException)
		{
			
		}

		protected RedisException(SerializationInfo info, StreamingContext context) 
			: base(info, context)
		{
		}

		public RedisException()
		{
		}
	}
}
