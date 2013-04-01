using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NBoosters.RedisBoost.Core.AsyncSocket;
using NBoosters.RedisBoost.Core.Serialization;
using NBoosters.RedisBoost.Misk;

namespace NBoosters.RedisBoost.Core.Receiver
{
	internal class RedisReceiver : IRedisReceiver
	{
		private const int BUFFER_SIZE = 1024 * 8;

		private readonly byte[] _buffer;
		private readonly IAsyncSocket _asyncSocket;
		private readonly IRedisSerializer _serializer;
		private readonly AsyncSocketEventArgs _socketArgs;

		#region context

		private ReceiverAsyncEventArgs _eventArgs;
		private int _multiBulkPartsLeft;
		private RedisResponse[] _multiBulkParts;
		private RedisResponse _redisResponse;
		private byte _firstChar;
		private readonly StringBuilder _lineBuffer;
		private byte[] _block;
		private int _readBytes;
		private int _offset;
		private int _bufferSize;
		#endregion

		public RedisReceiver(IAsyncSocket asyncSocket, IRedisSerializer serializer)
		{
			_buffer = new byte[BUFFER_SIZE];
			_asyncSocket = asyncSocket;
			_serializer = serializer;
			_socketArgs = new AsyncSocketEventArgs();
			_socketArgs.BufferToReceive = _buffer;
			_lineBuffer = new StringBuilder();
			_offset = 0;
		}

		public void EngageWith(ISocket socket)
		{
			_asyncSocket.EngageWith(socket);
		}

		public bool Receive(ReceiverAsyncEventArgs args)
		{
			_eventArgs = args;
			args.Error = null;
			args.Response = null;

			_multiBulkPartsLeft = 0;
			_multiBulkParts = null;

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
				return CallOperationCompleted(async,args);

			if (_multiBulkPartsLeft > 0)
				_multiBulkParts[_multiBulkParts.Length - _multiBulkPartsLeft] = _redisResponse;

			--_multiBulkPartsLeft;

			if (_multiBulkPartsLeft > 0)
				return ReadResponseFromStream(async,args);

			_redisResponse = RedisResponse.CreateMultiBulk(_multiBulkParts, _serializer);
			return CallOperationCompleted(async,args);
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
				else if (length == 0)
					_redisResponse = RedisResponse.CreateBulk(new byte[0], _serializer);
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
			
			return ReadLineTask(false, args);
		}

		private void ReadLineCallBack(AsyncSocketEventArgs args)
		{
			ReadLineCallBack(true, args);
		}
		private bool ReadLineCallBack(bool async,AsyncSocketEventArgs args)
		{
			RecalculateBufferSize(args);
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
					return _asyncSocket.Receive(args) || ReadLineCallBack(async, args);

				if (_buffer[_offset] == '\r')
				{
					_offset += 2;
					return CallSocketOpCompleted(async, args);
				}

				if (_firstChar == 0)
					_firstChar = _buffer[_offset];
				else
					_lineBuffer.Append((char)_buffer[_offset]);

				++_offset;
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
			RecalculateBufferSize(args);
			return ReadBlockLineTask(async, args);
		}
		private bool ReadBlockLineTask(bool async, AsyncSocketEventArgs args)
		{
			if (args.HasError)
				return CallSocketOpCompleted(async,args);

			while (_readBytes < _block.Length)
			{
				if (_offset >= _bufferSize)
					return _asyncSocket.Receive(args) || ReadBlockFromSocketCallBack(async, args);

				var bytesToCopy = _block.Length - _readBytes;

				var bytesInBufferLeft = _bufferSize - _offset;
				if (bytesToCopy > bytesInBufferLeft) bytesToCopy = bytesInBufferLeft;

				Array.Copy(_buffer, _offset, _block, _readBytes, bytesToCopy);

				_offset += bytesToCopy;
				_readBytes += bytesToCopy;
			}
			_offset += 2;
			return CallSocketOpCompleted(async,args);
		}
		#endregion
		private bool CallSocketOpCompleted(bool async, AsyncSocketEventArgs eventArgs)
		{
			_eventArgs.Error = eventArgs.Error;
			var callBack = (Action<AsyncSocketEventArgs>) eventArgs.UserToken;
			if (async) callBack(eventArgs);
			return async;
		}

		private bool CallOperationCompleted(bool async, AsyncSocketEventArgs eventArgs)
		{
			_eventArgs.Error = eventArgs.Error;
			_eventArgs.Response = _redisResponse;

			if (async) _eventArgs.Completed(_eventArgs);
			return async;
		}
	}
}
