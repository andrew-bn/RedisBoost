using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBoosters.RedisBoost.Extensions.ManualResetEvent
{
	public interface IManualResetEvent
	{
		void Set();
		void Reset();
		Task WaitOneAsync();
		Task<bool> WaitOneAsync(int millisecondsTimeout);
	}
}
