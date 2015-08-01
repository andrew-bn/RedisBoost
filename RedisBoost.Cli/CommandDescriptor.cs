namespace RedisBoost.Cli
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