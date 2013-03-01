using System.Threading.Tasks;
using NBooster.RedisBoost.Core;

namespace NBooster.RedisBoost
{
	public partial class RedisClient
	{
		public Task<string> AuthAsync(string password)
		{
			return StatusResponseCommand(RedisConstants.Auth, ConvertToByteArray(password));
		}
		public Task<byte[]> EchoAsync(byte[] message)
		{
			return BulkResponseCommand(RedisConstants.Echo, message);
		}
		public Task<string> PingAsync()
		{
			return StatusResponseCommand(RedisConstants.Ping);
		}
		Task IRedisSubscription.QuitAsync()
		{
			_state = ClientState.Quit;
			return ExecuteCommand(RedisConstants.Quit);
		}
		public Task<string> QuitAsync()
		{
			_state = ClientState.Quit;
			return StatusResponseCommand(RedisConstants.Quit);
		}
		public Task<string> SelectAsync(int index)
		{
			return StatusResponseCommand(RedisConstants.Select, ConvertToByteArray(index));
		}
	}
}
