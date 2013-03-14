using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NBoosters.RedisBoost.Core.AsyncSocket;
namespace NBoosters.RedisBoost.Core
{
	internal class RedisStream : IRedisStream
	{
		public const int CONNECTION_CLOSED = 0;
		private readonly IRedisDataAnalizer _redisDataAnalizer;
		private const int BUFFERS_SIZE = 1024 * 8;

		private readonly byte[] _writeBuffer;
		private volatile int _writeBufferOffset;

		private readonly byte[] _readBuffer;
		private volatile int _readBufferSize;
		private volatile int _readBufferOffset;

		private readonly SocketAsyncEventArgs _readArgs;
		private readonly SocketAsyncEventArgs _writeArgs;
		private readonly SocketAsyncEventArgs _notIoArgs;

		private volatile Socket _socket;

		public RedisStream(IRedisDataAnalizer redisDataAnalizer)
		{
			_redisDataAnalizer = redisDataAnalizer;
			_writeBuffer = new byte[BUFFERS_SIZE];
			_readBuffer = new byte[BUFFERS_SIZE];
			_writeBufferOffset = 0;

			_readArgs = new SocketAsyncEventArgs();

			_writeArgs = new SocketAsyncEventArgs();

			_notIoArgs = new SocketAsyncEventArgs();
		}

		public void EngageWith(Socket socket)
		{
			_socket = socket;
			_readArgs.AcceptSocket = _socket;
			_writeArgs.AcceptSocket = _socket;
			_notIoArgs.AcceptSocket = _socket;
		}


		private bool HasSpace(int dataLengthToWrite)
		{
			return (_writeBuffer.Length) >= (_writeBufferOffset + dataLengthToWrite);
		}
		public ArraySegment<byte> WriteData(ArraySegment<byte> data)
		{
			var bytesToWrite = _writeBuffer.Length - _writeBufferOffset;

			if (bytesToWrite == 0)
				return data;

			if (data.Count < bytesToWrite)
				bytesToWrite = data.Count;

			Array.Copy(data.Array, data.Offset, _writeBuffer, _writeBufferOffset, bytesToWrite);

			_writeBufferOffset += bytesToWrite;

			return new ArraySegment<byte>(data.Array, data.Offset + bytesToWrite, data.Count - bytesToWrite);
		}

		public bool WriteNewLine()
		{
			if (!HasSpace(2)) return false;
			WriteNewLineToBuffer();
			return true;
		}
		public bool WriteArgumentsCountLine(int argsCount)
		{
			return WriteCountLine(RedisConstants.Asterix, argsCount);
		}
		public bool WriteDataSizeLine(int argsCount)
		{
			return WriteCountLine(RedisConstants.Dollar, argsCount);
		}
		public bool WriteCountLine(byte startSimbol, int argsCount)
		{
			var part = _redisDataAnalizer.ConvertToByteArray(argsCount);
			int length = 3 + part.Length;

			if (!HasSpace(length)) return false;

			_writeBuffer[_writeBufferOffset++] = startSimbol;

			Array.Copy(part, 0, _writeBuffer, _writeBufferOffset, part.Length);
			_writeBufferOffset += part.Length;

			WriteNewLineToBuffer();

			return true;
		}

		public void ReadBlockLine(int length, Action<Exception, byte[]> callBack)
		{
			var result = new byte[length];
			var offset = 0;
			Action<Exception> body = null;
			body = ex =>
			{
				if (ex != null)
				{
					callBack(ex, null);
					return;
				}
				while (offset < length)
				{
					if (_readBufferOffset >= _readBufferSize)
					{
						ReadDataFromSocket(body);
						return;
					}

					var bytesToCopy = length - offset;

					if (_readBufferSize - _readBufferOffset < bytesToCopy)
						bytesToCopy = _readBufferSize - _readBufferOffset;

					Array.Copy(_readBuffer, _readBufferOffset, result, offset, bytesToCopy);
					_readBufferOffset += bytesToCopy;
					offset += bytesToCopy;
				}
				_readBufferOffset += 2;
				callBack(null, result);
			};

			body(null);
		}
		public void ReadLine(Action<Exception, RedisLine> callBack)
		{
			var sb = new StringBuilder();
			var result = new RedisLine();

			Action<Exception> body = null;
			body = ex =>
			{
				if (ex != null)
				{
					callBack(ex, result);
					return;
				}
				while (true)
				{
					if (_readBufferOffset >= _readBufferSize)
					{
						ReadDataFromSocket(body);
						return;
					}
					if (_readBuffer[_readBufferOffset] == '\r')
					{
						_readBufferOffset += 2;
						result.Line = sb.ToString();
						callBack(null, result);
						return;
					}

					if (result.FirstChar == 0)
						result.FirstChar = _readBuffer[_readBufferOffset];
					else
						sb.Append((char)_readBuffer[_readBufferOffset]);

					++_readBufferOffset;
				}
			};

			body(null);
		}
		private void WriteNewLineToBuffer()
		{
			_writeBuffer[_writeBufferOffset++] = RedisConstants.NewLine[0];
			_writeBuffer[_writeBufferOffset++] = RedisConstants.NewLine[1];
		}
		public void Flush(Action<Exception> callBack)
		{
			_writeArgs.SetBuffer(_writeBuffer, 0, _writeBufferOffset);
			_socket.SendAllAsync(_writeArgs,(ex,a)=>
							  {
								  _writeArgs.SetBuffer(null, 0, 0);
								  _writeBufferOffset = 0;
								  callBack(ex);
							  });
		}

		private void ReadDataFromSocket(Action<Exception> callBack)
		{			
			_readBufferOffset = _readBufferOffset - _readBufferSize;
			_readBufferSize = 0;
			_readArgs.SetBuffer(_readBuffer, 0, _readBuffer.Length);
			_socket.ReceiveAsyncAsync(_readArgs,(ex,a)=>
					{
						_readArgs.SetBuffer(null, 0, 0);
						_readBufferSize = _readArgs.BytesTransferred;
						callBack(ex);
					});
		}

		public Task Connect(EndPoint endPoint)
		{
			var tcs = new TaskCompletionSource<bool>();
			_notIoArgs.RemoteEndPoint = endPoint;
			_socket.ConnectAsyncAsync(_notIoArgs,(ex, a) =>
				{
					if (ex != null)
						tcs.SetException(ex);
					else tcs.SetResult(true);
				});
			return tcs.Task;
		}

		public Task Disconnect()
		{
			var tcs = new TaskCompletionSource<bool>();
			_socket.DisconnectAsyncAsync(_notIoArgs, (ex, a) =>
				{
					if (ex != null)
						tcs.SetException(ex);
					else tcs.SetResult(true);
				});
			return tcs.Task;
		}

		public void DisposeAndReuse()
		{
			_readBufferSize = 0;
			_readBufferOffset = 0;
			_writeBufferOffset = 0;

			_readArgs.AcceptSocket = null;
			_writeArgs.AcceptSocket = null;
			_notIoArgs.AcceptSocket = null;

			_socket.Dispose();
		}


		public bool BufferIsEmpty
		{
			get { return _writeBufferOffset == 0; }
		}
	}
}
