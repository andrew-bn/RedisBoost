namespace NBoosters.RedisBoost.Core.Receiver
{
	internal class ReceiverAsyncEventArgs : EventArgsBase<ReceiverAsyncEventArgs>
	{
		public RedisResponse Response { get; set; }
	}
}
