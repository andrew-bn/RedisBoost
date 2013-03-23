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
using System.Text;
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

		public bool ReadBlockLine(int length, AsyncOperationDelegate<Exception, byte[]> callBack)
		{
			var result = new byte[length];
			var offset = 0;
			AsyncOperationDelegate<Exception> body = null;
			body = (s, ex) =>
			{
				if (ex != null)
					return callBack(s, ex, null) && s;

				while (offset < length)
				{
					if (_readBufferOffset >= _readBufferSize)
						return ReadDataFromSocket(body) && s;

					var bytesToCopy = length - offset;

					if (_readBufferSize - _readBufferOffset < bytesToCopy)
						bytesToCopy = _readBufferSize - _readBufferOffset;

					Array.Copy(_readBuffer, _readBufferOffset, result, offset, bytesToCopy);
					_readBufferOffset += bytesToCopy;
					offset += bytesToCopy;
				}
				_readBufferOffset += 2;
				return callBack(s, null, result) && s;
			};

			return body(true, null);
		}
		public bool ReadLine(AsyncOperationDelegate<Exception, RedisLine> callBack)
		{
			var sb = new StringBuilder();
			var result = new RedisLine();

			AsyncOperationDelegate<Exception> body = null;
			body = (s, ex) =>
			{
				if (ex != null)
					return callBack(s, ex, result) && s;

				while (true)
				{
					if (_readBufferOffset >= _readBufferSize)
						return ReadDataFromSocket(body) && s;

					if (_readBuffer[_readBufferOffset] == '\r')
					{
						_readBufferOffset += 2;
						result.Line = sb.ToString();
						return callBack(s, null, result) && s;
					}

					if (result.FirstChar == 0)
						result.FirstChar = _readBuffer[_readBufferOffset];
					else
						sb.Append((char)_readBuffer[_readBufferOffset]);

					++_readBufferOffset;
				}
			};

			return body(true, null);
		}
		private void WriteNewLineToBuffer()
		{
			_writeBuffer[_writeBufferOffset++] = RedisConstants.NewLine[0];
			_writeBuffer[_writeBufferOffset++] = RedisConstants.NewLine[1];
		}
		public bool Flush(AsyncOperationDelegate<Exception> callBack)
		{
			_writeArgs.SetBuffer(_writeBuffer, 0, _writeBufferOffset);
			return _socket.SendAllAsync(_writeArgs, (s, ex) =>
							  {
								  _writeArgs.SetBuffer(null, 0, 0);
								  _writeBufferOffset = 0;
								  return callBack(s, ex) && s;
							  });
		}

		private bool ReadDataFromSocket(AsyncOperationDelegate<Exception> callBack)
		{
			_readBufferOffset = _readBufferOffset - _readBufferSize;
			_readBufferSize = 0;
			_readArgs.SetBuffer(_readBuffer, 0, _readBuffer.Length);
			return _socket.ReceiveAsyncAsync(_readArgs, (s, ex) =>
					{
						_readArgs.SetBuffer(null, 0, 0);
						_readBufferSize = _readArgs.BytesTransferred;
						return callBack(s, ex) && s;
					});
		}

		public bool Connect(EndPoint endPoint, AsyncOperationDelegate<Exception> callBack)
		{
			_notIoArgs.RemoteEndPoint = endPoint;
			return _socket.ConnectAsyncAsync(_notIoArgs, callBack);
		}

		public bool Disconnect(AsyncOperationDelegate<Exception> callBack)
		{
			return _socket.DisconnectAsyncAsync(_notIoArgs, callBack);
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
