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

using System;
using System.Net.Sockets;

namespace RedisBoost.Core.AsyncSocket
{
	internal class AsyncSocketWrapper: IAsyncSocket
	{
		private class SendAllContext
		{
			public int SentBytes { get; set; }
			public Action<SendAllContext> SendCallBack { get; set; }
			public AsyncSocketEventArgs EventArgs { get; set; }
			public SocketAsyncEventArgs SocketArgs { get; set; }
		}

		private  SendAllContext _sendAllContext;

		private ISocket _socket;
		public AsyncSocketWrapper()
		{
			_sendAllContext = new SendAllContext();
			_sendAllContext.SendCallBack = SendAllCallBack;
		}

		public void EngageWith(ISocket socket)
		{
			_socket = socket;
		}

		#region send
		public bool Send(AsyncSocketEventArgs eventArgs)
		{
			var args = new SocketAsyncEventArgs();
			args.Completed += SendCallBack;
			args.AcceptSocket = _socket.UnderlyingSocket;

			_sendAllContext.SentBytes = 0;
			_sendAllContext.EventArgs = eventArgs;
			_sendAllContext.SocketArgs = args;

			eventArgs.Error = null;

			args.BufferList = eventArgs.BufferList;

			args.UserToken = _sendAllContext;
			var isAsync = _socket.SendAsync(args);
			if (!isAsync) SendAllCallBack(false,_sendAllContext);
			return isAsync;
		}

		private bool SendAll(bool async, SocketAsyncEventArgs args)
		{
			var context = (SendAllContext) args.UserToken;

			if (context.SentBytes >= context.EventArgs.DataLength || context.EventArgs.HasError)
			{
				if (async) context.SendCallBack(context);
				return async;
			}

			args.SetBuffer(context.SentBytes, context.EventArgs.DataLength - context.SentBytes);

			return _socket.SendAsync(args) || SendCallBack(false,args);
		}

		private void SendCallBack(object sender, SocketAsyncEventArgs args)
		{
			SendCallBack(true,args);
		}

		private bool SendCallBack(bool async, SocketAsyncEventArgs args)
		{
			var context = (SendAllContext)args.UserToken;
			context.SentBytes += args.BytesTransferred;
			context.EventArgs.Error = GetExceptionIfError(args);
			if (async)
				context.SendCallBack(context);
			return async;// SendAll(async, args);
		}

		private void SendAllCallBack(SendAllContext context)
		{
			SendAllCallBack(true, context);
		}

		private void SendAllCallBack(bool async, SendAllContext context)
		{
			context.SocketArgs.SetBuffer(null,0,0);
			context.SocketArgs.BufferList = null;
			context.SocketArgs.UserToken = null;
			if (async) context.EventArgs.Completed(context.EventArgs);
		}

		#endregion

		#region receive
		public bool Receive(AsyncSocketEventArgs eventArgs)
		{
			var args = new SocketAsyncEventArgs();
			args.Completed += NotSendCallBack;
			
			args.SetBuffer(eventArgs.BufferToReceive, 0,eventArgs.BufferToReceive.Length);
			args.UserToken = eventArgs;
			eventArgs.Error = null;

			var isAsync = _socket.ReceiveAsync(args);
			if (!isAsync) NotSendCallBack(false, args);
			return isAsync;
		}

		#endregion
		#region Not IO operations
		public bool Connect(AsyncSocketEventArgs eventArgs)
		{
			var args = new SocketAsyncEventArgs();
			args.Completed += NotSendCallBack;
			
			args.RemoteEndPoint = eventArgs.RemoteEndPoint;
			args.UserToken = eventArgs;
			eventArgs.Error = null;

			var isAsync = _socket.ConnectAsync(args);
			if (!isAsync) NotSendCallBack(false, args);
			return isAsync;
		}

		public bool Disconnect(AsyncSocketEventArgs eventArgs)
		{
			var args = new SocketAsyncEventArgs();
			args.Completed += NotSendCallBack;

			args.UserToken = eventArgs;
			eventArgs.Error = null;

			var isAsync = _socket.DisconnectAsync(args);
			if (!isAsync) NotSendCallBack(false, args);
			return isAsync;
		}

		private void NotSendCallBack(object sender, SocketAsyncEventArgs args)
		{
			NotSendCallBack(true,args);
		}

		private void NotSendCallBack(bool async, SocketAsyncEventArgs args)
		{
			var token = (AsyncSocketEventArgs) args.UserToken;
			args.UserToken = null;
			token.Error = GetExceptionIfError(args);
			token.DataLength = args.BytesTransferred;

			if (args.LastOperation == SocketAsyncOperation.Receive ||
			    args.LastOperation == SocketAsyncOperation.Send)
				args.SetBuffer(null, 0, 0);

			if (async && token.Completed != null)
				token.Completed(token);
		}

		#endregion

		public Exception GetExceptionIfError(SocketAsyncEventArgs args)
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
	}
}
