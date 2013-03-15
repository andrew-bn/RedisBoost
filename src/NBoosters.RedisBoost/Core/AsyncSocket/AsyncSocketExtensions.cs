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
using System.Net.Sockets;

namespace NBoosters.RedisBoost.Core.AsyncSocket
{
	internal static class AsyncSocketExtensions
	{
		private static Exception GetExceptionIfError(SocketAsyncEventArgs args)
		{
			var error = args.SocketError;
			if (args.BytesTransferred == 0 &&
				(args.LastOperation == SocketAsyncOperation.Receive ||
				 args.LastOperation == SocketAsyncOperation.Send))
				error = SocketError.Shutdown;

			return error != SocketError.Success 
						? new SocketException((int)error) 
						: null;
		}

		public static void SendAllAsync(this Socket socket, SocketAsyncEventArgs args, Action<Exception> callBack)
		{
			SendAllAsync(socket, args, null, 0, callBack);
		}
		private static void SendAllAsync(this Socket socket, SocketAsyncEventArgs args, Exception exception, int sent, Action<Exception> callBack)
		{
			if (sent >= args.Count || exception != null)
			{
				callBack(exception);
				return;
			}

			int sendSize = args.Count - sent;
			if (sendSize > socket.SendBufferSize)
				sendSize = socket.SendBufferSize;

			args.SetBuffer( args.Offset + sent, sendSize);
			socket.SendAsyncAsync(args, ex => SendAllAsync(socket,args,ex,sent+args.BytesTransferred,callBack));
		}
		public static void SendAsyncAsync(this Socket socket, SocketAsyncEventArgs args, Action<Exception> callBack)
		{
			args.UserToken = callBack;
			args.Completed += AsyncOpCallBack;
			if (!socket.SendAsync(args))
				AsyncOpCallBack(socket, args);
		}
		public static void ReceiveAsyncAsync(this Socket socket, SocketAsyncEventArgs args, Action<Exception> callBack)
		{
			args.UserToken = callBack;
			args.Completed += AsyncOpCallBack;
			if (!socket.ReceiveAsync(args))
				AsyncOpCallBack(socket, args);
		}
		public static void ConnectAsyncAsync(this Socket socket, SocketAsyncEventArgs args,Action<Exception> callBack)
		{
			args.UserToken = callBack;
			args.Completed += AsyncOpCallBack;
			if (!socket.ConnectAsync(args))
				AsyncOpCallBack(socket, args);
		}
		public static void DisconnectAsyncAsync(this Socket socket, SocketAsyncEventArgs args, Action<Exception> callBack)
		{
			args.UserToken = callBack;
			args.Completed += AsyncOpCallBack;
			if (!socket.DisconnectAsync(args))
				AsyncOpCallBack(socket, args);
		}
		private static void AsyncOpCallBack(object sender, SocketAsyncEventArgs eventArgs)
		{
			eventArgs.Completed -= AsyncOpCallBack;
			var callBack = (Action<Exception>)eventArgs.UserToken;
			eventArgs.UserToken = null;

			var ex = GetExceptionIfError(eventArgs);
			callBack(ex);
		}
	}
}
