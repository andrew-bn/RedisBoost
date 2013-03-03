using System;
using System.Net;
using System.Threading.Tasks;

namespace NBoosters.RedisBoost
{
	public interface IRedisClientsPool: IDisposable
	{
		Task<IRedisClient> CreateClientAsync(string connectionString);
		Task<IRedisClient> CreateClientAsync(EndPoint endPoint);
		Task<IRedisClient> CreateClientAsync(EndPoint endPoint, int dbIndex);
	}
}