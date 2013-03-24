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
using NBoosters.RedisBoost.Core.AsyncSocket;

namespace NBoosters.RedisBoost.Core.RedisStream
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

		private volatile SocketAsyncEventArgs _readArgs;
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
			_readArgs.Completed += ReceiveAsyncOpCallBack;

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
			var length = 3 + part.Length;

			if (!HasSpace(length)) return false;

			_writeBuffer[_writeBufferOffset++] = startSimbol;

			Array.Copy(part, 0, _writeBuffer, _writeBufferOffset, part.Length);
			_writeBufferOffset += part.Length;

			WriteNewLineToBuffer();

			return true;
		}

		public bool ReadBlockLine(StreamAsyncEventArgs args)
		{
			_curReadStreamArgs = args;
			_curReadStreamArgs.Block = new byte[_curReadStreamArgs.Length];
			_curReadStreamArgs.Offset = 0;
			_curReadStreamArgs.Exception = null;
			return ReadBlockLineTask(false);
		}
		private bool CallOnCompleted(bool async)
		{
			if (async) _curReadStreamArgs.Completed(_curReadStreamArgs);
			return async;
		}
		private bool ReadBlockLineTask(bool async)
		{
			if (_curReadStreamArgs.HasException)
				return CallOnCompleted(async);

			while (_curReadStreamArgs.Offset < _curReadStreamArgs.Length)
			{
				if (_readBufferOffset >= _readBufferSize)
				{
					if (ReadDataFromSocket(()=>ReadBlockLineTask(true))) return true;
					if (_curReadStreamArgs.HasException)
						return CallOnCompleted(async);
					continue;
				}

				var bytesToCopy = _curReadStreamArgs.Length - _curReadStreamArgs.Offset;
				
				var bytesInBufferLeft = _readBufferSize - _readBufferOffset;
				if (bytesToCopy > bytesInBufferLeft) bytesToCopy = bytesInBufferLeft;

				Array.Copy(_readBuffer, _readBufferOffset, _curReadStreamArgs.Block,_curReadStreamArgs.Offset, bytesToCopy);
				
				_readBufferOffset += bytesToCopy;
				_curReadStreamArgs.Offset += bytesToCopy;
			}
			_readBufferOffset += 2;
			return CallOnCompleted(async);
		}

		private volatile StreamAsyncEventArgs _curReadStreamArgs;
		public bool ReadLine(StreamAsyncEventArgs args)
		{
			_curReadStreamArgs = args;
			_curReadStreamArgs.LineBuffer.Clear();
			_curReadStreamArgs.FirstChar = 0;
			_curReadStreamArgs.Line = null;
			_curReadStreamArgs.Exception = null;
			return ReadLineTask(false);
		}

		private bool ReadLineTask(bool async)
		{
			if (_curReadStreamArgs.HasException)
				return CallOnCompleted(async);

			while (true)
			{
				if (_readBufferOffset >= _readBufferSize)
				{
					if (ReadDataFromSocket(() => ReadLineTask(true))) return true;
					if (_curReadStreamArgs.HasException)
						return CallOnCompleted(async);
					continue;
				}

				if (_readBuffer[_readBufferOffset] == '\r')
				{
					_readBufferOffset += 2;
					_curReadStreamArgs.Line = _curReadStreamArgs.LineBuffer.ToString();
					return CallOnCompleted(async);
				}

				if (_curReadStreamArgs.FirstChar == 0)
					_curReadStreamArgs.FirstChar = _readBuffer[_readBufferOffset];
				else
					_curReadStreamArgs.LineBuffer.Append((char)_readBuffer[_readBufferOffset]);

				++_readBufferOffset;
			}
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

		private bool ReadDataFromSocket(Action callBack)
		{
			_readBufferOffset = _readBufferOffset - _readBufferSize;
			_readBufferSize = 0;
			_readArgs.SetBuffer(_readBuffer, 0, _readBuffer.Length);
			_readArgs.UserToken = callBack;

			var async = _socket.ReceiveAsync(_readArgs);
			if (!async) ReceiveAsyncOpCallBack(false, _socket, _readArgs);
			return async;
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

		private void ReceiveAsyncOpCallBack(object sender, SocketAsyncEventArgs eventArgs)
		{
			ReceiveAsyncOpCallBack(true, sender, eventArgs);
		}

		private void ReceiveAsyncOpCallBack(bool async, object sender, SocketAsyncEventArgs eventArgs)
		{
			_curReadStreamArgs.Exception = GetExceptionIfError(eventArgs);
			eventArgs.SetBuffer(null, 0, 0);
			_readBufferSize = eventArgs.BytesTransferred;

			var token = (Action) eventArgs.UserToken;
			eventArgs.UserToken = null;
			if (async) token();
		}

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

		private bool HasSpace(int dataLengthToWrite)
		{
			return (_writeBuffer.Length) >= (_writeBufferOffset + dataLengthToWrite);
		}
	}
}
