using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NBoosters.RedisBoost.Core.Serialization;

namespace NBoosters.RedisBoost.Core
{
	internal interface ISerializerDependent
	{
		void EngageWith(IRedisSerializer serializer);
	}
}
