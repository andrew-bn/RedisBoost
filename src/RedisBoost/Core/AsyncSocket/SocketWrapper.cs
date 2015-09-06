#region Apache Licence, Version 2.0
/*
 Copyright 2015 Andrey Bulygin.

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

using System.Net.Sockets;

namespace RedisBoost.Core.AsyncSocket
{
	internal sealed class SocketWrapper: ISocket
	{
		private readonly Socket _socket;

		public SocketWrapper(Socket socket)
		{
			_socket = socket;
		}

		public bool SendAsync(SocketAsyncEventArgs args)
		{
			return _socket.SendAsync(args);
		}

		public bool ReceiveAsync(SocketAsyncEventArgs args)
		{
			return _socket.ReceiveAsync(args);
		}

		public bool ConnectAsync(SocketAsyncEventArgs args)
		{
			return _socket.ConnectAsync(args);
		}

		public bool DisconnectAsync(SocketAsyncEventArgs args)
		{
			return _socket.DisconnectAsync(args);
		}

		public void Dispose()
		{
			_socket.Dispose();
		}

		public Socket UnderlyingSocket
		{
			get { return _socket; }
		}
	}
}
