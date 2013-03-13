using System;
using System.Net;
using System.Threading.Tasks;
using NBoosters.RedisBoost.Core;
using NBoosters.RedisBoost.Core.Serialization;

namespace NBoosters.RedisBoost
{
	public interface IRedisClientsPool: IDisposable
	{
		Task<IRedisClient> CreateClientAsync(string connectionString, BasicRedisSerializer serializer = null);
		Task<IRedisClient> CreateClientAsync(EndPoint endPoint, int dbIndex = 0, BasicRedisSerializer serializer = null);
		Task<IRedisClient> CreateClientAsync(string host, int port, int dbIndex = 0, BasicRedisSerializer serializer = null);
	}
}