using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NBoosters.RedisBoost.Core.AsyncSocket;

namespace NBoosters.RedisBoost.Core
{
	internal interface ISocketDependent
	{
		void EngageWith(ISocket socket);
	}
}
