using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBoosters.RedisBoost.Core.Serialization
{
	public interface IRedisSerializer
	{
		byte[] Serialize(object value);
		object Deserialize(Type type, byte[] value);
	}
}
