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
using System.Text;
using RedisBoost.Core.AsyncSocket;
using RedisBoost.Core.Serialization;
using RedisBoost.Misk;

namespace RedisBoost.Core.Receiver
{
	internal class RedisReceiver : IRedisReceiver
	{
		private IRedisSerializer _serializer;
		private readonly IBuffersPool _buffersPool;
		private readonly IAsyncSocket _asyncSocket;
		private volatile byte[] _readSocketBuffer;
		private readonly AsyncSocketEventArgs _socketArgs;

		#region context
		private ReceiverAsyncEventArgs _curEventArgs;
		private volatile int _multiBulkPartsLeft;
		private volatile RedisResponse[] _multiBulkParts;
		private volatile RedisResponse _redisResponse;
		private volatile byte _firstChar;
		private readonly StringBuilder _lineBuffer;
		private volatile byte[] _block;
		private volatile int _readBytes;
		private volatile int _offset;
		private volatile int _bufferSize;
		#endregion

		public RedisReceiver(IBuffersPool buffersPool, IAsyncSocket asyncSocket)
		{
			_buffersPool = buffersPool;
			_asyncSocket = asyncSocket;
			_socketArgs = new AsyncSocketEventArgs();
			_lineBuffer = new StringBuilder();
			_offset = 0;
		}

		public void EngageWith(ISocket socket)
		{
			_offset = 0;
			_bufferSize = 0;
			_asyncSocket.EngageWith(socket);
		}

		public bool Receive(ReceiverAsyncEventArgs args)
		{
			_curEventArgs = args;
			args.Error = null;
			args.Response = null;

			_multiBulkPartsLeft = 0;
			_multiBulkParts = null;
			_redisResponse = null;

			return ReadResponseFromStream(false, _socketArgs);
		}

		private bool ReadResponseFromStream(bool async, AsyncSocketEventArgs args)
		{
			args.Completed = ProcessRedisLine;
			return ReadLine(args) || ProcessRedisLine(async, args);
		}

		private bool ProcessRedisResponse(bool async, AsyncSocketEventArgs args)
		{
			if (args.HasError || _multiBulkParts == null)
				return CallOperationCompleted(async, args);

			if (_multiBulkPartsLeft > 0)
				_multiBulkParts[_multiBulkParts.Length - _multiBulkPartsLeft] = _redisResponse;

			--_multiBulkPartsLeft;

			if (_multiBulkPartsLeft > 0)
				return ReadResponseFromStream(async, args);

			_redisResponse = RedisResponse.CreateMultiBulk(_multiBulkParts, _serializer);
			return CallOperationCompleted(async, args);
		}

		private void ProcessRedisBulkLine(AsyncSocketEventArgs args)
		{
			ProcessRedisBulkLine(true, args);
		}

		private bool ProcessRedisBulkLine(bool async, AsyncSocketEventArgs args)
		{
			if (args.HasError)
				return ProcessRedisResponse(async, args);

			_redisResponse = RedisResponse.CreateBulk(_block, _serializer);
			return ProcessRedisResponse(async, args);
		}

		private void ProcessRedisLine(AsyncSocketEventArgs args)
		{
			ProcessRedisLine(true, args);
		}

		private bool ProcessRedisLine(bool async, AsyncSocketEventArgs args)
		{
			var lineValue = _lineBuffer.ToString();
			if (args.HasError)
				return ProcessRedisResponse(async, args);

			if (_firstChar.IsErrorReply())
				_redisResponse = RedisResponse.CreateError(lineValue, _serializer);
			else if (_firstChar.IsStatusReply())
				_redisResponse = RedisResponse.CreateStatus(lineValue, _serializer);
			else if (_firstChar.IsIntReply())
				_redisResponse = RedisResponse.CreateInteger(lineValue.ToInt(), _serializer);
			else if (_firstChar.IsBulkReply())
			{
				var length = lineValue.ToInt();
				//check nil reply
				if (length < 0)
					_redisResponse = RedisResponse.CreateBulk(null, _serializer);
				else
				{
					args.Completed = ProcessRedisBulkLine;
					return ReadBlockLine(length, args) || ProcessRedisBulkLine(async, args);
				}
			}
			else if (_firstChar.IsMultiBulkReply())
			{
				_multiBulkPartsLeft = lineValue.ToInt();

				if (_multiBulkPartsLeft == -1) // multi-bulk nill
					_redisResponse = RedisResponse.CreateMultiBulk(null, _serializer);
				else
				{
					_multiBulkParts = new RedisResponse[_multiBulkPartsLeft];

					if (_multiBulkPartsLeft > 0)
						return ReadResponseFromStream(async, args);
				}
			}

			return ProcessRedisResponse(async, args);
		}
		#region read line
		private bool ReadLine(AsyncSocketEventArgs args)
		{
			_firstChar = 0;
			_lineBuffer.Clear();
			args.DataLength = 0;

			args.UserToken = args.Completed;
			args.Completed = ReadLineCallBack;
			args.Error = null;
			return ReadLineTask(false, args);
		}

		private void ReadLineCallBack(AsyncSocketEventArgs args)
		{
			ReadLineCallBack(true, args);
		}
		private bool ReadLineCallBack(bool async, AsyncSocketEventArgs args)
		{
			args.BufferToReceive = null;
			RecalculateBufferSize(args);

			if (args.HasError)
				ReleaseBuffer();

			return ReadLineTask(async, args);
		}



		private void RecalculateBufferSize(AsyncSocketEventArgs args)
		{
			_offset = _offset - _bufferSize;
			_bufferSize = args.DataLength;
		}

		private bool ReadLineTask(bool async, AsyncSocketEventArgs args)
		{
			if (args.HasError)
				return CallSocketOpCompleted(async, args);

			while (true)
			{
				if (_offset >= _bufferSize)
					return ReceiveDataFromSocket(args) || ReadLineCallBack(async, args);

				var dataByte = _readSocketBuffer[_offset];
				IncrementOffset(1);
				if (dataByte == '\r')
				{
					IncrementOffset(1);
					return CallSocketOpCompleted(async, args);
				}

				if (_firstChar == 0)
					_firstChar = dataByte;
				else
					_lineBuffer.Append((char)dataByte);
			}
		}

		#endregion

		#region read block
		private bool ReadBlockLine(int length, AsyncSocketEventArgs args)
		{
			_block = new byte[length];
			_readBytes = 0;
			args.Error = null;
			args.UserToken = args.Completed;
			args.Completed = ReadBlockFromSocketCallBack;
			args.DataLength = 0;
			return ReadBlockLineTask(false, args);
		}

		private void ReadBlockFromSocketCallBack(AsyncSocketEventArgs args)
		{
			ReadBlockFromSocketCallBack(true, args);
		}
		private bool ReadBlockFromSocketCallBack(bool async, AsyncSocketEventArgs args)
		{
			args.BufferToReceive = null;
			RecalculateBufferSize(args);
			if (args.HasError)
				ReleaseBuffer();
			return ReadBlockLineTask(async, args);
		}

		private bool ReadBlockLineTask(bool async, AsyncSocketEventArgs args)
		{
			if (args.HasError)
				return CallSocketOpCompleted(async, args);

			while (_readBytes < _block.Length)
			{
				if (_offset >= _bufferSize)
					return ReceiveDataFromSocket(args) || ReadBlockFromSocketCallBack(async, args);

				var bytesToCopy = _block.Length - _readBytes;

				var bytesInBufferLeft = _bufferSize - _offset;
				if (bytesToCopy > bytesInBufferLeft) bytesToCopy = bytesInBufferLeft;

				Array.Copy(_readSocketBuffer, _offset, _block, _readBytes, bytesToCopy);

				_readBytes += bytesToCopy;
				IncrementOffset(bytesToCopy);
			}
			IncrementOffset(2);
			return CallSocketOpCompleted(async, args);
		}
		#endregion
		private void ReleaseBuffer()
		{
			_buffersPool.Release(_readSocketBuffer);
			_readSocketBuffer = null;
		}

		private bool ReceiveDataFromSocket(AsyncSocketEventArgs args)
		{
			byte[] buffer;
			if (_buffersPool.TryGet(out buffer, b => ReceiveDataFromSocket(b, args, true)))
				return ReceiveDataFromSocket(buffer, args, false);

			return true;
		}

		private bool ReceiveDataFromSocket(byte[] buffer, AsyncSocketEventArgs args, bool invokeCompletedCallback)
		{
			_readSocketBuffer = buffer;
			args.BufferToReceive = _readSocketBuffer;
			try
			{
				var async = _asyncSocket.Receive(args);
				if (!async && invokeCompletedCallback)
					args.Completed(args);
				return async;
			}
			catch (Exception ex)
			{
				args.Error = ex;
				if (invokeCompletedCallback)
					args.Completed(args);
				return false;
			}
		}

		private void IncrementOffset(int value)
		{
			_offset += value;
			if (_offset >= _bufferSize && _readSocketBuffer != null)
				ReleaseBuffer();
		}

		private bool CallSocketOpCompleted(bool async, AsyncSocketEventArgs eventArgs)
		{
			_curEventArgs.Error = eventArgs.Error;
			var callBack = (Action<AsyncSocketEventArgs>)eventArgs.UserToken;
			if (async) callBack(eventArgs);
			return async;
		}

		private bool CallOperationCompleted(bool async, AsyncSocketEventArgs eventArgs)
		{
			_curEventArgs.Error = eventArgs.Error;
			_curEventArgs.Response = _redisResponse;

			if (async) _curEventArgs.Completed(_curEventArgs);
			return async;
		}

		public void EngageWith(IRedisSerializer serializer)
		{
			_serializer = serializer;
		}
	}
}