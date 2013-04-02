namespace NBoosters.RedisBoost
{
	internal enum ClientState
	{
		None,
		Connect,
		Subscription,
		Disconnect,
		Quit,
		FatalError,
	}
}