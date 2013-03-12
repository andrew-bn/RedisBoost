using System.Threading.Tasks;
using NBoosters.RedisBoost.Core;

namespace NBoosters.RedisBoost
{
	public partial class RedisClient
	{
		public Task<string> AuthAsync(string password)
		{
			return StatusResponseCommand(RedisConstants.Auth, ConvertToByteArray(password));
		}
		public Task<Bulk> EchoAsync(byte[] message)
		{
			return BulkResponseCommand(RedisConstants.Echo, message);
		}
		public Task<string> PingAsync()
		{
			return StatusResponseCommand(RedisConstants.Ping);
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
