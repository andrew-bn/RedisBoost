using System;
using System.Net;
using System.Threading.Tasks;
using NBoosters.RedisBoost.Core;

namespace NBoosters.RedisBoost
{
	public interface IRedisClientsPool: IDisposable
	{
		Task<IRedisClient> CreateClientAsync(string connectionString);
		Task<IRedisClient> CreateClientAsync(EndPoint endPoint, int dbIndex = 0);
		Task<IRedisClient> CreateClientAsync(string host, int port = RedisConstants.DefaultPort, int dbIndex = 0);
	}
}