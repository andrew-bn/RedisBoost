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

namespace NBoosters.RedisBoost.Core.RedisStream
{
	internal class RedisStream : IRedisStream
	{
		private const int BUFFERS_SIZE = 1024 * 8;

		private readonly IRedisDataAnalizer _redisDataAnalizer;

		// fields used for data sending
		private int _sentBytes;
		private readonly byte[] _writeBuffer;
		private int _writeBufferOffset;

		// fields used fo data receiving
		private int _read;
		private readonly byte[] _readBuffer;
		private int _readBufferSize;
		private int _readBufferOffset;
		public StringBuilder _readLineBuffer;

		private readonly SocketAsyncEventArgs _readArgs;
		private readonly SocketAsyncEventArgs _writeArgs;
		private readonly SocketAsyncEventArgs _notIoArgs;

		private StreamAsyncEventArgs _curSendStreamArgs;
		private StreamAsyncEventArgs _curReadStreamArgs;

		private Socket _socket;

		private Action _sendCallBack;

		public RedisStream(IRedisDataAnalizer redisDataAnalizer)
		{
			_redisDataAnalizer = redisDataAnalizer;
			_writeBuffer = new byte[BUFFERS_SIZE];
			_readBuffer = new byte[BUFFERS_SIZE];

			_readArgs = new SocketAsyncEventArgs();
			_readArgs.Completed += ReceiveAsyncOpCallBack;

			_writeArgs = new SocketAsyncEventArgs();
			_writeArgs.Completed += SendCallBack;

			_notIoArgs = new SocketAsyncEventArgs();

			_readLineBuffer = new StringBuilder();
		}

		public void EngageWith(Socket socket)
		{
			_socket = socket;
			_readArgs.AcceptSocket = _socket;
			_writeArgs.AcceptSocket = _socket;
			_notIoArgs.AcceptSocket = _socket;
		}

		#region writing
		public bool Flush(StreamAsyncEventArgs args)
		{
			_curSendStreamArgs = args;
			Func<bool, bool> sendCallBack = (async) =>
			{
				_writeArgs.SetBuffer(null, 0, 0);
				_writeBufferOffset = 0;
				return CallOnSendCompleted(async);
			};
			return SendAllAsync(() => sendCallBack(true)) || sendCallBack(false);
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
		#endregion
		public bool ReadBlockLine(StreamAsyncEventArgs args)
		{
			_curReadStreamArgs = args;
			_curReadStreamArgs.Block = new byte[_curReadStreamArgs.Length];
			_curReadStreamArgs.Exception = null;

			_read = 0;
			return ReadBlockLineTask(false);
		}
		private bool CallOnReadCompleted(bool async)
		{
			if (async) _curReadStreamArgs.Completed(_curReadStreamArgs);
			return async;
		}
		private bool CallOnSendCompleted(bool async)
		{
			if (async) _curSendStreamArgs.Completed(_curReadStreamArgs);
			return async;
		}
		private bool ReadBlockLineTask(bool async)
		{
			if (_curReadStreamArgs.HasException)
				return CallOnReadCompleted(async);

			while (_read < _curReadStreamArgs.Length)
			{
				if (_readBufferOffset >= _readBufferSize)
				{
					if (ReadDataFromSocket(()=>ReadBlockLineTask(true))) return true;
					if (_curReadStreamArgs.HasException)
						return CallOnReadCompleted(async);
					continue;
				}

				var bytesToCopy = _curReadStreamArgs.Length - _read;
				
				var bytesInBufferLeft = _readBufferSize - _readBufferOffset;
				if (bytesToCopy > bytesInBufferLeft) bytesToCopy = bytesInBufferLeft;

				Array.Copy(_readBuffer, _readBufferOffset, _curReadStreamArgs.Block, _read, bytesToCopy);
				
				_readBufferOffset += bytesToCopy;
				_read += bytesToCopy;
			}
			_readBufferOffset += 2;
			return CallOnReadCompleted(async);
		}

		public bool ReadLine(StreamAsyncEventArgs args)
		{
			_curReadStreamArgs = args;
			_readLineBuffer.Clear();
			_curReadStreamArgs.FirstChar = 0;
			_curReadStreamArgs.Line = null;
			_curReadStreamArgs.Exception = null;
			return ReadLineTask(false);
		}

		private bool ReadLineTask(bool async)
		{
			if (_curReadStreamArgs.HasException)
				return CallOnReadCompleted(async);

			while (true)
			{
				if (_readBufferOffset >= _readBufferSize)
				{
					if (ReadDataFromSocket(() => ReadLineTask(true))) return true;
					if (_curReadStreamArgs.HasException)
						return CallOnReadCompleted(async);
					continue;
				}

				if (_readBuffer[_readBufferOffset] == '\r')
				{
					_readBufferOffset += 2;
					_curReadStreamArgs.Line = _readLineBuffer.ToString();
					return CallOnReadCompleted(async);
				}

				if (_curReadStreamArgs.FirstChar == 0)
					_curReadStreamArgs.FirstChar = _readBuffer[_readBufferOffset];
				else
					_readLineBuffer.Append((char)_readBuffer[_readBufferOffset]);

				++_readBufferOffset;
			}
		}

		private void WriteNewLineToBuffer()
		{
			_writeBuffer[_writeBufferOffset++] = RedisConstants.NewLine[0];
			_writeBuffer[_writeBufferOffset++] = RedisConstants.NewLine[1];
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
			_curReadStreamArgs.Exception = eventArgs.GetExceptionIfError();
			eventArgs.SetBuffer(null, 0, 0);
			_readBufferSize = eventArgs.BytesTransferred;
			var token = (Action) eventArgs.UserToken;
			eventArgs.UserToken = null;
			if (async) token();
		}

		#region helpers
		private bool HasSpace(int dataLengthToWrite)
		{
			return (_writeBuffer.Length) >= (_writeBufferOffset + dataLengthToWrite);
		}

		private bool SendAllAsync(Action callBack)
		{
			_sentBytes = 0;
			_sendCallBack = callBack;
			_writeArgs.SetBuffer(_writeBuffer, 0, _writeBufferOffset);
			return SendAllAsync(false);
		}

		private bool SendAllAsync(bool async)
		{
			var ex = _writeArgs.GetExceptionIfError();
			if (_sentBytes >= _writeBufferOffset || ex != null)
			{
				if (async) _sendCallBack();
				return async;
			}

			_writeArgs.SetBuffer(_sentBytes, _writeBufferOffset - _sentBytes);

			Func<bool, bool> sendPacketCallBack =
				isAsync =>
				{
					_sentBytes += _writeArgs.BytesTransferred;
					return SendAllAsync(isAsync);
				};
			return SendPacketAsync(() => sendPacketCallBack(true)) || sendPacketCallBack(async);
		}

		private bool SendPacketAsync(Action callBack)
		{
			_writeArgs.UserToken = callBack;
			var async = _socket.SendAsync(_writeArgs);
			if (!async) SendCallBack(false,_socket,_writeArgs);
			return async;
		}

		private void SendCallBack(object sender, SocketAsyncEventArgs args)
		{
			SendCallBack(true,sender,args);
		}

		private void SendCallBack(bool async, object sender, SocketAsyncEventArgs args)
		{
			var action = (Action)_writeArgs.UserToken;
			_writeArgs.UserToken = null;
			if (async) action();
		}
		#endregion
	}
}
