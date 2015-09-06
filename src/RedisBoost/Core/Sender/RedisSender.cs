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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using RedisBoost.Core.AsyncSocket;
using RedisBoost.Misk;

namespace RedisBoost.Core.Sender
{
	internal class RedisSender : IRedisSender
	{
		private const int ADDITIONAL_LINE_BYTES = 3; // first char [*$+-] and \r\n
		private class SenderContext
		{
			public int SendState { get; set; }
			public int PartIndex { get; set; }
			public SenderAsyncEventArgs EventArgs { get; set; }
			public ArraySegment<byte> ArraySegment { get; set; }
			public Action<SenderAsyncEventArgs> CallBack { get; set; }
		}

		private readonly ConcurrentQueue<ArraySegment<byte>> _sendingQueue;

		private byte[] _writingBuffer;
		private int _writingOffset;
		private int _writingBufferIsFlushed = 1;

		private readonly IBuffersPool _buffersPool;
		private readonly IAsyncSocket _asyncSocket;
		private readonly bool _autoFlush;
		private readonly AsyncSocketEventArgs _flushArgs;
		private readonly SenderContext _senderContext;
		private int _flushingInProgress;
		private Exception _socketException;

		public RedisSender(IBuffersPool buffersPool, IAsyncSocket asyncSocket, bool autoFlush)
		{
			_buffersPool = buffersPool;
			_asyncSocket = asyncSocket;
			_autoFlush = autoFlush;
			_senderContext = new SenderContext();

			_flushArgs = new AsyncSocketEventArgs();
			_flushArgs.Completed = FlushCallBack;

			_sendingQueue = new ConcurrentQueue<ArraySegment<byte>>();
		}

		public bool Send(SenderAsyncEventArgs args)
		{
			args.Error = _socketException;

			_senderContext.SendState = 0;
			_senderContext.PartIndex = 0;
			_senderContext.EventArgs = args;
			_senderContext.CallBack = args.Completed;

			args.Completed = SendDataTask;
			args.UserToken = _senderContext;

			return SendDataTask(false, args);
		}

		private void SendDataTask(SenderAsyncEventArgs args)
		{
			SendDataTask(true, args);
		}

		private bool SendDataTask(bool async, SenderAsyncEventArgs args)
		{
			var context = (SenderContext)args.UserToken;

		TryAgain:

			if (context.EventArgs.HasError)
				return CallOnSendCompleted(async, context);

			if (context.SendState == 0)
			{
				var writeResult = WriteArgumentsCountLine(context.EventArgs.DataToSend.Length);
				if (!writeResult.HasValue) return true;
				if (!writeResult.Value)
				{
					if (Flush(context.EventArgs)) return true;
					goto TryAgain;
				}
				context.SendState = 1;
			}

			if (context.SendState > 0 && context.SendState < 4)
			{
				for (; context.PartIndex < context.EventArgs.DataToSend.Length; context.PartIndex++)
				{
					if (context.SendState == 1)
					{
						var writeResult = WriteDataSizeLine(context.EventArgs.DataToSend[context.PartIndex].Length);
						if (!writeResult.HasValue) return true;
						if (!writeResult.Value)
						{
							if (Flush(context.EventArgs)) return true;
							goto TryAgain;
						}
						context.SendState = 2;
						context.ArraySegment = new ArraySegment<byte>(context.EventArgs.DataToSend[context.PartIndex], 0,
																	  context.EventArgs.DataToSend[context.PartIndex].Length);
					}

					if (context.SendState == 2)
					{
						var writeResult = WriteData(context.ArraySegment);
						if (!writeResult.HasValue) return true;
						context.ArraySegment = writeResult.Value;
						if (context.ArraySegment.Count > 0)
						{
							if (Flush(context.EventArgs)) return true;
							goto TryAgain;
						}

						context.SendState = 3;
					}

					if (context.SendState == 3)
					{
						var writeResult = WriteNewLineToBuffer();
						if (!writeResult.HasValue) return true;
						if (!writeResult.Value)
						{
							if (Flush(context.EventArgs)) return true;
							goto TryAgain;
						}
						context.SendState = 1;
					}
				}
			}

			if (context.SendState != 4 && _autoFlush)
			{
				context.SendState = 4;
				if (Flush(context.EventArgs)) return true;
				goto TryAgain;
			}

			return CallOnSendCompleted(async, context);
		}

		private static bool CallOnSendCompleted(bool async, SenderContext context)
		{
			context.EventArgs.Completed = context.CallBack;
			if (async) context.CallBack(context.EventArgs);
			return async;
		}

		public bool Flush(SenderAsyncEventArgs args)
		{
			if (_socketException != null)
			{
				args.Error = _socketException;
				return false;
			}
			if (_writingBufferIsFlushed == 0)
			{
				_sendingQueue.Enqueue(new ArraySegment<byte>(_writingBuffer, 0, _writingOffset));
				Interlocked.Exchange(ref _writingBufferIsFlushed, 1);
			}

			EnterFlushing();
			return false;
		}

		private void EnterFlushing()
		{
			if (Interlocked.CompareExchange(ref _flushingInProgress, 1, 0) == 0)
				DoFlushing();
		}
		private void DoFlushing()
		{
			var buffers = new List<ArraySegment<byte>>();

			while (true)
			{
				buffers.Clear();

				ArraySegment<byte> sendingBuffer;
				while (_sendingQueue.TryDequeue(out sendingBuffer))
					buffers.Add(sendingBuffer);

				if (buffers.Count > 0)
				{
					if (CallAsyncSocketSend(buffers)) 
						return;
					FlushCallBack(false, _flushArgs);
					continue;
				}

				if (LeaveFlushing())
					break;
			}
		}
		private bool CallAsyncSocketSend(IList<ArraySegment<byte>> buffers)
		{
			try
			{
				_flushArgs.Error = _socketException;
				_flushArgs.BufferList = buffers;
				_flushArgs.UserToken = buffers;
				return !_flushArgs.HasError && _asyncSocket.Send(_flushArgs);
			}
			catch (Exception ex)
			{
				_flushArgs.Error = ex;
				return false;
			}
		}
		private bool LeaveFlushing()
		{
			Interlocked.Exchange(ref _flushingInProgress, 0);
			return _sendingQueue.Count == 0 || Interlocked.CompareExchange(ref _flushingInProgress, 1, 0) != 0;
		}

		private void FlushCallBack(AsyncSocketEventArgs args)
		{
			FlushCallBack(true, args);
		}

		private void FlushCallBack(bool async, AsyncSocketEventArgs args)
		{
			var buffers = (List<ArraySegment<byte>>)args.UserToken;

			Interlocked.Exchange(ref _socketException, args.Error);

			foreach (var buf in buffers)
				_buffersPool.ReleaseWithoutNotification(buf.Array);

			if (async) DoFlushing();
			
			_buffersPool.NotifyWaiters();
		}

		public void EngageWith(ISocket socket)
		{
			Interlocked.Exchange(ref _flushingInProgress, 0);
			Interlocked.Exchange(ref _writingOffset, 0);
			Interlocked.Exchange(ref _socketException, null);
			_asyncSocket.EngageWith(socket);
		}

		#region write helpers

		private bool? WriteArgumentsCountLine(int argsCount)
		{
			return WriteCountLine(RedisConstants.Asterix, argsCount);
		}
		private bool? WriteDataSizeLine(int argsCount)
		{
			return WriteCountLine(RedisConstants.Dollar, argsCount);
		}

		private bool? WriteCountLine(byte startSimbol, int argsCount)
		{
			var part = argsCount.ToBytes();
			return WriteLineToBuffer(startSimbol, part, 0, part.Length);
		}

		private ArraySegment<byte>? WriteData(ArraySegment<byte> data)
		{
			if (!GetNewWritingBuffer())
				return null;

			var bytesToWrite = _writingBuffer.Length - _writingOffset;

			if (bytesToWrite == 0)
				return data;

			if (data.Count < bytesToWrite)
				bytesToWrite = data.Count;

			if (bytesToWrite > 0)
				Array.Copy(data.Array, data.Offset, _writingBuffer, _writingOffset, bytesToWrite);
			
			_writingOffset += bytesToWrite;

			return new ArraySegment<byte>(data.Array, data.Offset + bytesToWrite, data.Count - bytesToWrite);
		}

		private bool? WriteLineToBuffer(byte startSimbol, byte[] lineData, int sourceOffset, int length)
		{
			if (!GetNewWritingBuffer())
				return null;

			if (!HasSpace(length + ADDITIONAL_LINE_BYTES)) return false;
			_writingBuffer[_writingOffset++] = startSimbol;
			Array.Copy(lineData, sourceOffset, _writingBuffer, _writingOffset, length);

			_writingOffset += length;

			WriteNewLineToBuffer();

			return true;
		}
		private bool? WriteNewLineToBuffer()
		{
			if (!GetNewWritingBuffer())
				return null;

			if (!HasSpace(2)) return false;
			_writingBuffer[_writingOffset++] = RedisConstants.NewLine[0];
			_writingBuffer[_writingOffset++] = RedisConstants.NewLine[1];
			return true;
		}

		private bool HasSpace(int dataLengthToWrite)
		{
			return _writingBufferIsFlushed == 0 && (_writingBuffer.Length) >= (_writingOffset + dataLengthToWrite);
		}
		private bool GetNewWritingBuffer()
		{
			if (_writingBufferIsFlushed == 0)
				return true;

			byte[] buffer;

			if (!_buffersPool.TryGet(out buffer, b => ApplyNewWritingBuffer(true,b)))
				return false;

			ApplyNewWritingBuffer(false, buffer);
			return true;
		}

		private void ApplyNewWritingBuffer(bool async, byte[] buffer)
		{
			Interlocked.Exchange(ref _writingBuffer, buffer);
			Interlocked.Exchange(ref _writingOffset, 0);
			Interlocked.Exchange(ref _writingBufferIsFlushed, 0);

			if (async)
			{
				_senderContext.EventArgs.Error = _socketException;
				_senderContext.EventArgs.Completed(_senderContext.EventArgs);
			}
		}
		#endregion

		public int BytesInBuffer
		{
			get { return _writingBufferIsFlushed==0? _writingOffset:0; }
		}
	}
}
