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
		void Flush(Action<Exception> callBack);
		ArraySegment<byte> WriteData(ArraySegment<byte> data);
		bool WriteArgumentsCountLine(int argsCount);
		bool WriteNewLine();
		bool WriteDataSizeLine(int argsCount);
		bool WriteCountLine(byte startSimbol, int argsCount);
		void ReadBlockLine(int length, Action<Exception, byte[]> callBack);
		void ReadLine(Action<Exception, RedisLine> callBack);
		void Connect(EndPoint endPoint, Action<Exception> callBack);
		void Disconnect(Action<Exception> callBack);
	}
}