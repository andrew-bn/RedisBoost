using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NBoosters.RedisBoost.Core
{
	internal interface IRedisStream
	{
		void EngageWith(Socket socket);
		void DisposeAndReuse();
		bool BufferIsEmpty { get; }
		Task Flush();
		ArraySegment<byte> WriteData(ArraySegment<byte> data);
		bool WriteArgumentsCountLine(int argsCount);
		bool WriteNewLine();
		bool WriteDataSizeLine(int argsCount);
		bool WriteCountLine(byte startSimbol, int argsCount);
		Task<byte[]> ReadBlockLine(int length);
		Task<RedisLine> ReadLine();
		Task Connect(EndPoint endPoint);
		Task Disconnect();
	}
}