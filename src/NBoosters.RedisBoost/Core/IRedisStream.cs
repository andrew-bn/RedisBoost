#region Apache Licence, Version 2.0
/*
 Copyright 2013 Andrey Bulygin.

 Licensed under the Apache License, Version 2.0 (the "License"); 
 you may not use this file except in compliance with the License. 
 You may obtain a copy of the License at 

		http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software 
 distributed under the License is distributed on an "AS IS" BASIS, 
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
 See the License for the specific language governing permissions 
 and limitations under the License.
 */
#endregion

using System;
using System.Net;
using System.Net.Sockets;

namespace NBoosters.RedisBoost.Core
{
	internal interface IRedisStream
	{
		void EngageWith(Socket socket);
		void DisposeAndReuse();
		bool BufferIsEmpty { get; }
		bool Flush(AsyncOperationDelegate<Exception> callBack);
		ArraySegment<byte> WriteData(ArraySegment<byte> data);
		bool WriteArgumentsCountLine(int argsCount);
		bool WriteNewLine();
		bool WriteDataSizeLine(int argsCount);
		bool WriteCountLine(byte startSimbol, int argsCount);
		bool ReadBlockLine(int length, AsyncOperationDelegate<Exception, byte[]> callBack);
		bool ReadLine(AsyncOperationDelegate<Exception, RedisLine> callBack);
		bool Connect(EndPoint endPoint, AsyncOperationDelegate<Exception> callBack);
		bool Disconnect(AsyncOperationDelegate<Exception> callBack);
	}
}