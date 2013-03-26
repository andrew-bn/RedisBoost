using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace NBoosters.RedisBoost.Core.AsyncSocket
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

		private SendAllContext _sendAllContext;
		private SocketAsyncEventArgs _sendArgs;
		private SocketAsyncEventArgs _receiveArgs;
		private SocketAsyncEventArgs _notIoArgs;

		private ISocket _socket;

		public void EngageWith(ISocket socket)
		{
			_socket = socket;

			_notIoArgs = new SocketAsyncEventArgs();
			_notIoArgs.Completed += NotSendCallBack;

			_receiveArgs = new SocketAsyncEventArgs();
			_receiveArgs.Completed += NotSendCallBack;

			_sendArgs = new SocketAsyncEventArgs();
			_sendArgs.Completed += SendCallBack;

			_sendAllContext = new SendAllContext();
			_sendAllContext.SendCallBack = SendAllCallBack;
			_sendAllContext.SocketArgs = _sendArgs;
		}

		#region send
		public bool Send(AsyncSocketEventArgs eventArgs)
		{
			_sendAllContext.SentBytes = 0;
			_sendAllContext.EventArgs = eventArgs;
			eventArgs.Error = null;

			_sendArgs.SetBuffer(eventArgs.DataToSend, 0, eventArgs.DataLength);
			_sendArgs.UserToken = _sendAllContext;
			var isAsync = SendAll(false, _sendArgs);
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
			return SendAll(async,args);
		}

		private void SendAllCallBack(SendAllContext context)
		{
			SendAllCallBack(true, context);
		}

		private void SendAllCallBack(bool async, SendAllContext context)
		{
			context.SocketArgs.SetBuffer(null,0,0);
			context.SocketArgs.UserToken = null;
			if (async) context.EventArgs.Completed(context.EventArgs);
		}

		#endregion

		#region receive
		public bool Receive(AsyncSocketEventArgs eventArgs)
		{
			_receiveArgs.SetBuffer(eventArgs.BufferToReceive, 0,eventArgs.BufferToReceive.Length);
			_receiveArgs.UserToken = eventArgs;
			eventArgs.Error = null;

			var isAsync = _socket.ReceiveAsync(_receiveArgs);
			if (!isAsync) NotSendCallBack(false, _receiveArgs);
			return isAsync;
		}

		#endregion
		#region Not IO operations
		public bool Connect(AsyncSocketEventArgs eventArgs)
		{
			_notIoArgs.RemoteEndPoint = eventArgs.RemoteEndPoint;
			_notIoArgs.UserToken = eventArgs;
			eventArgs.Error = null;

			var isAsync = _socket.ConnectAsync(_notIoArgs);
			if (!isAsync) NotSendCallBack(false, _notIoArgs);
			return isAsync;
		}

		public bool Disconnect(AsyncSocketEventArgs eventArgs)
		{
			_notIoArgs.UserToken = eventArgs;
			eventArgs.Error = null;

			var isAsync = _socket.DisconnectAsync(_notIoArgs);
			if (!isAsync) NotSendCallBack(false, _notIoArgs);
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
