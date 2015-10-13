using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using RedisBoost.Core;

namespace RedisBoost
{
	public partial class RedisClient
	{
		public Task<string> ReadonlyAsync()
		{
			return StatusCommand(RedisConstants.Readonly);
		}

		public Task<string> ReadWriteAsync()
		{
			return StatusCommand(RedisConstants.ReadWrite);
		}
	}
}
