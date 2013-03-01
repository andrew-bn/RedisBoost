using System.Net;
using System.Threading.Tasks;

namespace NBooster.RedisBoost
{
	public interface IRedisClientsPool
	{
		Task<IRedisClient> CreateClient(string connectionString);
		Task<IRedisClient> CreateClient(EndPoint endPoint);
		Task<IRedisClient> CreateClient(EndPoint endPoint, int dbIndex);
	}
}