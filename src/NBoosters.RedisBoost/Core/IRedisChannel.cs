using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NBoosters.RedisBoost.Core
{
	internal interface IRedisChannel : IDisposable
	{
		IRedisDataAnalizer RedisDataAnalizer { get; }
		void EngageWith(Socket socket);
		Task SendAsync(params byte[][] args);
		Task<RedisResponse> ReadResponseAsync();
		Task ConnectAsync(EndPoint endPoint);
		Task DisconnectAsync();
		Task Flush();
	}
}
