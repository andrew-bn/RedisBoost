using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NBoosters.RedisBoost.Core.Serialization;

namespace NBoosters.RedisBoost.Core
{
	internal interface IRedisChannel : IDisposable
	{
		IRedisDataAnalizer RedisDataAnalizer { get; }
		void EngageWith(Socket socket, IRedisSerializer serializer);
		void SendAsync(byte[][] request, Action<Exception> callback);
		void ReadResponseAsync(Action<Exception,RedisResponse> callBack);
		void ConnectAsync(EndPoint endPoint, Action<Exception> callBack);
		void DisconnectAsync(Action<Exception> callBack);
		void Flush(Action<Exception> callBack);

		bool BufferIsEmpty { get; }
	}
}
