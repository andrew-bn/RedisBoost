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

		public static bool SendAllAsync(this Socket socket, SocketAsyncEventArgs args, AsyncOperationDelegate<Exception> callBack)
		{
			return SendAllAsync(socket, true, args, null, 0, callBack);
		}
		private static bool SendAllAsync(this Socket socket, bool sync, SocketAsyncEventArgs args, Exception exception, int sent, AsyncOperationDelegate<Exception> callBack)
		{
			if (sent >= args.Count || exception != null)
				return sync && callBack(sync,exception);
			

			int sendSize = args.Count - sent;
			if (sendSize > socket.SendBufferSize)
				sendSize = socket.SendBufferSize;

			args.SetBuffer( args.Offset + sent, sendSize);
			return socket.SendAsyncAsync(args, (s,ex) => sync && SendAllAsync(socket, s, args,ex,sent+args.BytesTransferred,callBack));
		}
		public static bool SendAsyncAsync(this Socket socket, SocketAsyncEventArgs args, AsyncOperationDelegate<Exception> callBack)
		{
			args.UserToken = callBack;
			args.Completed += AsyncOpCallBack;
			return !socket.SendAsync(args) && AsyncOpCallBack(true, socket, args);
		}
		public static bool ReceiveAsyncAsync(this Socket socket, SocketAsyncEventArgs args, AsyncOperationDelegate<Exception> callBack)
		{
			args.UserToken = callBack;
			args.Completed += AsyncOpCallBack;
			return !socket.ReceiveAsync(args) && AsyncOpCallBack(true, socket, args);
		}
		public static bool ConnectAsyncAsync(this Socket socket, SocketAsyncEventArgs args, AsyncOperationDelegate<Exception> callBack)
		{
			args.UserToken = callBack;
			args.Completed += AsyncOpCallBack;
			return !socket.ConnectAsync(args) && AsyncOpCallBack(true, socket, args);
		}
		public static bool DisconnectAsyncAsync(this Socket socket, SocketAsyncEventArgs args, AsyncOperationDelegate<Exception> callBack)
		{
			args.UserToken = callBack;
			args.Completed += AsyncOpCallBack;
			return !socket.DisconnectAsync(args) && AsyncOpCallBack(true, socket, args);
		}
		private static void AsyncOpCallBack(object sender, SocketAsyncEventArgs eventArgs)
		{
			AsyncOpCallBack(false,sender,eventArgs);
		}
		private static bool AsyncOpCallBack(bool sync, object sender, SocketAsyncEventArgs eventArgs)
		{
			eventArgs.Completed -= AsyncOpCallBack;

			var callBack = (AsyncOperationDelegate<Exception>)eventArgs.UserToken;
			eventArgs.UserToken = null;

			var ex = GetExceptionIfError(eventArgs);
			return sync && callBack(sync, ex);
		}
	}
}
