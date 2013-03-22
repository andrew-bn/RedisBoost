using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NBoosters.RedisBoost.Core
{
	internal delegate bool AsyncOperationDelegate<T>(bool sync, T arg);
	internal delegate bool AsyncOperationDelegate<T1, T2>(bool sync, T1 arg1, T2 arg2);
}
