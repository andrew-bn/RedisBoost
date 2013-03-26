using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NBoosters.RedisBoost.Core
{
	internal class EventArgsBase<T>
	{
		public Exception Error { get; set; }
		public bool HasError { get { return Error != null; } }
		public Action<T> Completed { get; set; }
		public object UserToken { get; set; }
	}
}
