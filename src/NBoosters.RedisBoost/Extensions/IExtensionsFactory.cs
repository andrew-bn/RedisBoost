using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NBoosters.RedisBoost.Extensions.ManualResetEvent;
using NBoosters.RedisBoost.Extensions.Queue;

namespace NBoosters.RedisBoost.Extensions
{
	public interface IExtensionsFactory
	{
		IQueue<T> CreateQueue<T>(string name);
		IManualResetEvent CreateManualResetEvent(string name);
	}
}
