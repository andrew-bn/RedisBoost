using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBoosters.RedisBoost.Core.Serialization
{
	internal interface IRedisSerializer
	{
		byte[] Serialize<T>(T value);

	}
}
