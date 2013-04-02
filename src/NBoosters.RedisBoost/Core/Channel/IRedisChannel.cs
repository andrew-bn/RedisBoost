using System;
using System.Net;
using System.Threading.Tasks;

namespace NBoosters.RedisBoost.Core.Channel
{
	internal interface IRedisChannel: ISocketDependent, ISerializerDependent, IDisposable
	{
		Task<MultiBulk> MultiBulkCommand(byte[][] args);
		Task<string> StatusCommand(byte[][] args);
		Task<long> IntegerCommand(byte[][] args);
		Task<long?> IntegerOrBulkNullCommand(byte[][] args);
		Task<Bulk> BulkCommand(params byte[][] args);
		Task SendDirectRequest(byte[][] args);
		Task<RedisResponse> ReadDirectResponse();

		Task Connect(EndPoint endPoint);
		Task Disconnect();
	}
}
