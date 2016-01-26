using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisBoost.Cmd
{
	public class CommandDescriptor
	{
		public string Name { get; private set; }
		public object[] Arguments { get; private set; }

		public CommandDescriptor(string name, params object[] arguments)
		{
			Name = name;
			Arguments = arguments;
		}
	}
}
