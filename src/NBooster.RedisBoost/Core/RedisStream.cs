using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NBooster.RedisBoost.Core.AsyncSocket;
namespace NBooster.RedisBoost.Core
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

		public async Task<byte[]> ReadBlockLine(int length)
		{
			var result = new byte[length];
			var offset = 0;
			while (offset < length)
			{
				if (_readBufferOffset >= _readBufferSize)
					await ReadDataFromSocket().ConfigureAwait(false);

				var bytesToCopy = length - offset;

				if (_readBufferSize - _readBufferOffset < bytesToCopy)
					bytesToCopy = _readBufferSize - _readBufferOffset;

				Array.Copy(_readBuffer, _readBufferOffset, result, offset, bytesToCopy);
				_readBufferOffset += bytesToCopy;
				offset += bytesToCopy;
			}
			_readBufferOffset += 2;
			return result;
		}

		public async Task<RedisLine> ReadLine()
		{
			var sb = new StringBuilder();
			var result = new RedisLine();

			while (true)
			{
				if (_readBufferOffset >= _readBufferSize)
					await ReadDataFromSocket().ConfigureAwait(false);

				if (_readBuffer[_readBufferOffset] == '\r')
				{
					_readBufferOffset += 2;
					break;
				}

				if (result.FirstChar == 0)
					result.FirstChar = _readBuffer[_readBufferOffset];
				else
					sb.Append((char)_readBuffer[_readBufferOffset]);

				++_readBufferOffset;
			}

			result.Line = sb.ToString();
			return result;
		}

		private void WriteNewLineToBuffer()
		{
			_writeBuffer[_writeBufferOffset++] = RedisConstants.NewLine[0];
			_writeBuffer[_writeBufferOffset++] = RedisConstants.NewLine[1];
		}
		public Task Flush()
		{
			_writeArgs.SetBuffer(_writeBuffer, 0, _writeBufferOffset);
			return _socket.SendAllAsync(_writeArgs)
						  .ContinueWith(t =>
							  {
								  _writeArgs.SetBuffer(null, 0, 0);
								  _writeBufferOffset = 0;
								  if (t.IsFaulted) throw t.Exception;
							  });
		}

		private Task ReadDataFromSocket()
		{
			_readBufferOffset = _readBufferOffset - _readBufferSize;
			_readBufferSize = 0;
			_readArgs.SetBuffer(_readBuffer, 0, _readBuffer.Length);
			return _socket.ReceiveAsyncAsync(_readArgs)
					.ContinueWith((t) =>
					{
						_readArgs.SetBuffer(null, 0, 0);
						_readBufferSize = _readArgs.BytesTransferred;
						if (t.IsFaulted) throw t.Exception;
					});
		}

		public Task Connect(EndPoint endPoint)
		{
			_notIoArgs.RemoteEndPoint = endPoint;
			return _socket.ConnectAsyncAsync(_notIoArgs);
		}

		public Task Disconnect()
		{
			return _socket.DisconnectAsyncAsync(_notIoArgs);
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
	}
}
