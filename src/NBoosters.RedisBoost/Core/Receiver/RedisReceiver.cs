//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using NBoosters.RedisBoost.Core.AsyncSocket;

//namespace NBoosters.RedisBoost.Core.Receiver
//{
//	internal class RedisReceiver: IRedisReceiver
//	{
//		private class ReceiverContext
//		{
//			public ReceiverContext()
//			{
//				LineBuffer = new StringBuilder();
//			}

//			public int MultiBulkPartsLeft { get; set; }
//			public RedisResponse[] MultiBulkParts { get; set; }
//			public ReceiverAsyncEventArgs EventArgs { get; set; }
//			public int FirstChar { get; set; }
//			public StringBuilder LineBuffer { get; private set; }

//			public byte[] Block { get; set; }

//			public int ReadBlockBytes { get; set; }
//		}
//		private const int BUFFER_SIZE = 1024 * 8;

//		private readonly byte[] _buffer;
//		private int _offset;
//		private int _bufferSize;
//		private readonly IAsyncSocket _asyncSocket;
//		private readonly AsyncSocketEventArgs _receiveArgs;
//		private readonly ReceiverContext _receiveContext;

//		public RedisReceiver(IAsyncSocket asyncSocket)
//		{
//			_asyncSocket = asyncSocket;
//			_receiveArgs = new AsyncSocketEventArgs();
//			_receiveArgs.BufferToReceive = _buffer;
//			_receiveContext = new ReceiverContext();
//			_buffer = new byte[BUFFER_SIZE];
//			_offset = 0;
//		}

//		public void EngageWith(ISocket socket)
//		{
//			_asyncSocket.EngageWith(socket);
//		}

//		public bool Receive(ReceiverAsyncEventArgs args)
//		{
//			args.Error = null;
//			args.Response = null;

//			_receiveContext.MultiBulkPartsLeft = 0;
//			_receiveContext.MultiBulkParts = null;
//			_receiveContext.EventArgs = args;

//			_receiveArgs.UserToken = _receiveContext;

//			return ReadResponseFromStream(false, _receiveArgs);
//		}

//		private bool ReadResponseFromStream(bool async, AsyncSocketEventArgs args)
//		{
//			args.Completed = ProcessRedisLine;
//			return _redisStream.ReadLine(_readStreamArgs) || ProcessRedisLine(async, _readStreamArgs);
//		}

//		private bool ProcessRedisResponse(bool async)
//		{
//			if (_curReadChannelArgs.Exception != null || _multiBulkParts == null)
//				return CallOnReadCompleted(async);

//			if (_receiveMultiBulkPartsLeft > 0)
//				_multiBulkParts[_multiBulkParts.Length - _receiveMultiBulkPartsLeft] = _curReadChannelArgs.RedisResponse;

//			--_receiveMultiBulkPartsLeft;

//			if (_receiveMultiBulkPartsLeft > 0)
//				return ReadResponseFromStream(async);

//			_curReadChannelArgs.RedisResponse = RedisResponse.CreateMultiBulk(_multiBulkParts, _serializer);
//			return CallOnReadCompleted(async);
//		}

//		private void ProcessRedisBulkLine(StreamAsyncEventArgs args)
//		{
//			ProcessRedisBulkLine(true, args);
//		}

//		private bool ProcessRedisBulkLine(bool async, StreamAsyncEventArgs args)
//		{
//			_curReadChannelArgs.Exception = args.Exception;

//			if (args.HasException)
//				return ProcessRedisResponse(async);

//			_curReadChannelArgs.RedisResponse = RedisResponse.CreateBulk(args.Block, _serializer);
//			return ProcessRedisResponse(async);
//		}

//		private void ProcessRedisLine(StreamAsyncEventArgs streamArgs)
//		{
//			ProcessRedisLine(true, streamArgs);
//		}

//		private bool ProcessRedisLine(bool async, StreamAsyncEventArgs streamArgs)
//		{
//			_curReadChannelArgs.Exception = streamArgs.Exception;

//			if (streamArgs.HasException)
//				return ProcessRedisResponse(async);

//			if (_redisDataAnalizer.IsErrorReply(streamArgs.FirstChar))
//				_curReadChannelArgs.RedisResponse = RedisResponse.CreateError(streamArgs.Line, _serializer);
//			else if (_redisDataAnalizer.IsStatusReply(streamArgs.FirstChar))
//				_curReadChannelArgs.RedisResponse = RedisResponse.CreateStatus(streamArgs.Line, _serializer);
//			else if (_redisDataAnalizer.IsIntReply(streamArgs.FirstChar))
//				_curReadChannelArgs.RedisResponse = RedisResponse.CreateInteger(_redisDataAnalizer.ConvertToLong(streamArgs.Line), _serializer);
//			else if (_redisDataAnalizer.IsBulkReply(streamArgs.FirstChar))
//			{
//				var length = _redisDataAnalizer.ConvertToInt(streamArgs.Line);
//				//check nil reply
//				if (length < 0)
//					_curReadChannelArgs.RedisResponse = RedisResponse.CreateBulk(null, _serializer);
//				else if (length == 0)
//					_curReadChannelArgs.RedisResponse = RedisResponse.CreateBulk(new byte[0], _serializer);
//				else
//				{
//					_readStreamArgs.Length = length;
//					_readStreamArgs.Completed = ProcessRedisBulkLine;
//					return _redisStream.ReadBlockLine(_readStreamArgs) ||
//							ProcessRedisBulkLine(async, _readStreamArgs);
//				}
//			}
//			else if (_redisDataAnalizer.IsMultiBulkReply(streamArgs.FirstChar))
//			{
//				_receiveMultiBulkPartsLeft = _redisDataAnalizer.ConvertToInt(streamArgs.Line);

//				if (_receiveMultiBulkPartsLeft == -1) // multi-bulk nill
//					_curReadChannelArgs.RedisResponse = RedisResponse.CreateMultiBulk(null, _serializer);
//				else
//				{
//					_multiBulkParts = new RedisResponse[_receiveMultiBulkPartsLeft];

//					if (_receiveMultiBulkPartsLeft > 0)
//						return ReadResponseFromStream(async);
//				}
//			}

//			return ProcessRedisResponse(async);
//		}
//		#region read line
//		public bool ReadLine(AsyncSocketEventArgs args)
//		{
//			var ctx = (ReceiverContext) args.UserToken;
//			ctx.FirstChar = 0;
//			ctx.LineBuffer.Clear();
//			args.Completed = ReadLineCallBack;
//			return ReadLineTask(false, args);
//		}

//		private void ReadLineCallBack(AsyncSocketEventArgs args)
//		{
//			ReadLineTask(true, args);
//		}

//		private bool ReadLineTask(bool async, AsyncSocketEventArgs args)
//		{
//			var ctx = (ReceiverContext) args.UserToken;

//			if (args.HasError)
//				return CallCompleted(async,args);

//			while (true)
//			{
//				if (_offset >= _bufferSize)
//				{
//					if (_asyncSocket.Receive(args)) return true;
//					if (args.HasError) return CallCompleted(async,args);
//					continue;
//				}

//				if (_buffer[_offset] == '\r')
//				{
//					_offset += 2;
//					return CallCompleted(async,args);
//				}

//				if (ctx.FirstChar == 0)
//					ctx.FirstChar = _buffer[_offset];
//				else
//					ctx.LineBuffer.Append((char)_buffer[_offset]);

//				++_offset;
//			}
//		}
//		private bool CallCompleted<T>(bool async, T eventArgs) where T: EventArgsBase<T>
//		{
//			if (async) eventArgs.Completed(eventArgs);
//			return async;
//		}
//		#endregion
//		#region read block
//		public bool ReadBlockLine(AsyncSocketEventArgs args)
//		{
//			var ctx = (ReceiverContext)args.UserToken;
//			ctx.Block = new byte[args.DataLength];
//			ctx.ReadBlockBytes = 0;
//			args.Error = null;
//			args.Completed = ReadBlockLineTask;
//			return ReadBlockLineTask(false, args);
//		}

//		private void ReadBlockLineTask(AsyncSocketEventArgs args)
//		{
//			ReadBlockLineTask(true, args);
//		}

//		private bool ReadBlockLineTask(bool async, AsyncSocketEventArgs args)
//		{
//			var ctx = (ReceiverContext) args.UserToken;
//			if (args.HasError)
//				return CallCompleted(async,args);

//			while (ctx.ReadBlockBytes < args.)
//			{
//				if (_readBufferOffset >= _readBufferSize)
//				{
//					if (ReadDataFromSocket(() => ReadBlockLineTask(true))) return true;
//					if (_curReadStreamArgs.HasException)
//						return CallOnReadCompleted(async);
//					continue;
//				}

//				var bytesToCopy = _curReadStreamArgs.Length - _read;

//				var bytesInBufferLeft = _readBufferSize - _readBufferOffset;
//				if (bytesToCopy > bytesInBufferLeft) bytesToCopy = bytesInBufferLeft;

//				Array.Copy(_readBuffer, _readBufferOffset, _curReadStreamArgs.Block, _read, bytesToCopy);

//				_readBufferOffset += bytesToCopy;
//				_read += bytesToCopy;
//			}
//			_readBufferOffset += 2;
//			return CallOnReadCompleted(async);
//		}
//		#endregion
//	}
//}
