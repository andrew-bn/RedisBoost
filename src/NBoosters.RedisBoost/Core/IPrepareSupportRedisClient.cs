using System.Threading.Tasks;

namespace NBoosters.RedisBoost.Core
{
	internal interface IPrepareSupportRedisClient: IRedisClient
	{
		RedisClient.ClientState State { get; }
		Task PrepareClientConnection();
	}
}
