using System.Net.Sockets;
using System.Threading.Tasks;

namespace NBoosters.RedisBoost.Core.AsyncSocket
{
	internal static class AsyncSocketExtensions
	{
		private static bool SetErrorIfException<T>(TaskCompletionSource<T> tcs, SocketAsyncEventArgs args)
		{
			var error = args.SocketError;
			if (args.BytesTransferred == 0 &&
				(args.LastOperation == SocketAsyncOperation.Receive ||
				 args.LastOperation == SocketAsyncOperation.Send))
				error = SocketError.Shutdown;

			if (error != SocketError.Success)
			{
				tcs.SetException(new SocketException((int)error));
				return true;
			}
			return false;
		}
		public static async Task SendAllAsync(this Socket socket, SocketAsyncEventArgs args)
		{
			int sent = 0;
			int length = args.Count;
			int offset = args.Offset;
			while (sent < length)
			{
				int sendSize = length - sent;
				if (sendSize > socket.SendBufferSize)
					sendSize = socket.SendBufferSize;

				args.SetBuffer(offset + sent, sendSize);
				await socket.SendAsyncAsync(args).ConfigureAwait(false);
				sent += args.BytesTransferred;
			}
		}

		public static Task SendAsyncAsync(this Socket socket, SocketAsyncEventArgs args)
		{
			var tcs = new TaskCompletionSource<object>();
			args.UserToken = tcs;
			args.Completed += AsyncOpCallBack;
			if (!socket.SendAsync(args))
				AsyncOpCallBack(socket, args);
			return tcs.Task;
		}
		public static Task ReceiveAsyncAsync(this Socket socket, SocketAsyncEventArgs args)
		{
			var tcs = new TaskCompletionSource<object>();
			args.UserToken = tcs;
			args.Completed += AsyncOpCallBack;
			if (!socket.ReceiveAsync(args))
				AsyncOpCallBack(socket, args);
			return tcs.Task;
		}
		public static Task ConnectAsyncAsync(this Socket socket, SocketAsyncEventArgs args)
		{
			var tcs = new TaskCompletionSource<object>();
			args.UserToken = tcs;
			args.Completed += AsyncOpCallBack;
			if (!socket.ConnectAsync(args))
				AsyncOpCallBack(socket, args);
			return tcs.Task;
		}
		public static Task DisconnectAsyncAsync(this Socket socket, SocketAsyncEventArgs args)
		{
			var tcs = new TaskCompletionSource<object>();
			args.UserToken = tcs;
			args.Completed += AsyncOpCallBack;
			if (!socket.DisconnectAsync(args))
				AsyncOpCallBack(socket, args);
			return tcs.Task;
		}
		private static void AsyncOpCallBack(object sender, SocketAsyncEventArgs eventArgs)
		{
			eventArgs.Completed -= AsyncOpCallBack;
			var tcs = (TaskCompletionSource<object>)eventArgs.UserToken;
			eventArgs.UserToken = null;

			if (!SetErrorIfException(tcs, eventArgs))
				tcs.SetResult(null);
		}
	}
}
