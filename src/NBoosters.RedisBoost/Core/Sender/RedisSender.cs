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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NBoosters.RedisBoost.Core.AsyncSocket;
using NBoosters.RedisBoost.Misk;

namespace NBoosters.RedisBoost.Core.Sender
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

		private const int BUFFER_SIZE = 1024 * 8;

		private ConcurrentQueue<ArraySegment<byte>> _sendingQueue;

		private byte[] _writingBuffer;
		private int _writingOffset;

		private readonly IBuffersPool _buffersPool;
		private readonly IAsyncSocket _asyncSocket;
		private readonly bool _autoFlush;
		private readonly AsyncSocketEventArgs _flushArgs;
		private readonly SenderContext _senderContext;
		private int _flushInQueue;
		private Exception _socketException;

		public RedisSender(IBuffersPool buffersPool,IAsyncSocket asyncSocket, bool autoFlush)
			: this(buffersPool, asyncSocket, BUFFER_SIZE, autoFlush)
		{}

		public RedisSender(IBuffersPool buffersPool, IAsyncSocket asyncSocket, int bufferSize, bool autoFlush)
		{
			_buffersPool = buffersPool;
			_asyncSocket = asyncSocket;
			_autoFlush = autoFlush;
			_senderContext = new SenderContext();

			_flushArgs = new AsyncSocketEventArgs();
			_flushArgs.Completed = FlushCallBack;

			_writingBuffer = new byte[bufferSize];

			_sendingQueue = new ConcurrentQueue<ArraySegment<byte>>();
		}

		public bool Send(SenderAsyncEventArgs args)
		{
			args.Error = null;

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

			if (context.EventArgs.HasError)
				return CallOnSendCompleted(async, context);

			if (context.SendState == 0)
			{
				while (!WriteArgumentsCountLine(context.EventArgs.DataToSend.Length))
					if (Flush(context.EventArgs)) return true;
				context.SendState = 1;
			}

			if (context.SendState > 0 && context.SendState < 4)
			{
				for (; context.PartIndex < context.EventArgs.DataToSend.Length; context.PartIndex++)
				{
					if (context.SendState == 1)
					{
						while (!WriteDataSizeLine(context.EventArgs.DataToSend[context.PartIndex].Length))
							if (Flush(context.EventArgs)) return true;

						context.SendState = 2;
						context.ArraySegment = new ArraySegment<byte>(context.EventArgs.DataToSend[context.PartIndex], 0,
																	  context.EventArgs.DataToSend[context.PartIndex].Length);
					}

					if (context.SendState == 2)
					{
						while (true)
						{
							context.ArraySegment = WriteData(context.ArraySegment);
							if (context.ArraySegment.Count > 0)
							{
								if (Flush(context.EventArgs)) return true;
								continue;
							}

							context.SendState = 3;
							break;
						}
					}

					if (context.SendState == 3)
					{
						while (!WriteNewLine())
							if (Flush(context.EventArgs)) return true;
						context.SendState = 1;
					}
				}
			}

			if (context.SendState != 4 && _autoFlush)
			{
				context.SendState = 4;
				if (Flush(context.EventArgs)) return true;
			}

			return CallOnSendCompleted(async, context);
		}

		private bool CallOnSendCompleted(bool async, SenderContext context)
		{
			context.EventArgs.Completed = context.CallBack;
			if (async) context.CallBack(context.EventArgs);
			return async;
		}

		private SenderAsyncEventArgs _flushingArgs;
		private int _isWaiting = 0;
		private int _flushed = 0;
		public bool Flush(SenderAsyncEventArgs args)
		{
			int queueSize = 0;
			bool hasWritingBuffer = false;

			lock (this)
			{
				if (_flushed==0)
					queueSize = Interlocked.Increment(ref _flushInQueue);
				EnqueueFlush();
				hasWritingBuffer = CreateWritingBuffer();
				if (!hasWritingBuffer)
				{
					Interlocked.Exchange(ref _isWaiting, 1);
					_flushingArgs = args;
				}
			}

			if (!hasWritingBuffer)
				return true;
			if (queueSize == 0)
				return false;
			if (queueSize > 1)
				return false;
			if (queueSize == 1)
			{
				DoFlushing();
				return false;
			}

			throw new InvalidOperationException("Invalid flush state");
		}

		private void EnqueueFlush()
		{
			if (_flushed == 1)
				return;

			_sendingQueue.Enqueue(new ArraySegment<byte>(_writingBuffer,0,_writingOffset));
			Interlocked.Exchange(ref _flushed, 1);
		}

		private bool CreateWritingBuffer()
		{
			byte[] buffer;
			if (!_buffersPool.TryGet(out buffer))
				return false;

			Interlocked.Exchange(ref _writingBuffer, buffer);
			Interlocked.Exchange(ref _writingOffset, 0);
			Interlocked.Exchange(ref _flushed, 0);
			return true;
		}
		private bool DoFlushing()
		{
			ArraySegment<byte> sendingBuffer;
			var asyncExecution = false;
			
			var buffers =new List<ArraySegment<byte>>();
			
			NextIteration:
			
			buffers.Clear();
			
			while (_sendingQueue.TryDequeue(out sendingBuffer))
				buffers.Add(sendingBuffer);

			if (buffers.Count > 0)
			{
				_flushArgs.Error = _socketException;
				_flushArgs.DataToSend = null;
				_flushArgs.BufferList = buffers;
				_flushArgs.UserToken = buffers;

				var isAsync = _asyncSocket.Send(_flushArgs);
				asyncExecution = asyncExecution || isAsync;

				if (isAsync)
					return asyncExecution;

				FlushCallBack(false, _flushArgs);

				goto NextIteration;
			}
			return asyncExecution;
		}

		private void FlushCallBack(AsyncSocketEventArgs args)
		{
			FlushCallBack(true, args);
		}

		private void FlushCallBack(bool async, AsyncSocketEventArgs args)
		{
			lock (this)
			{
				var buffers = (List<ArraySegment<byte>>) args.UserToken;

				Interlocked.Exchange(ref _socketException, args.Error);
				_flushInQueue -= buffers.Count;

				foreach (var buf in buffers)
					_buffersPool.Release(buf.Array);

				if (_flushInQueue > 0 && async) // flush was pending. call back should be called
					DoFlushing();

				if (async && Interlocked.CompareExchange(ref _isWaiting, 0, 1) == 1)
					_flushingArgs.Completed(_flushingArgs);
			}
		}


		public void EngageWith(ISocket socket)
		{
			Interlocked.Exchange(ref _socketException, null);
			_asyncSocket.EngageWith(socket);
		}

		#region write helpers
		private bool WriteNewLine()
		{
			if (!HasSpace(2)) return false;
			WriteNewLineToBuffer();
			return true;
		}
		private bool WriteArgumentsCountLine(int argsCount)
		{
			return WriteCountLine(RedisConstants.Asterix, argsCount);
		}
		private bool WriteDataSizeLine(int argsCount)
		{
			return WriteCountLine(RedisConstants.Dollar, argsCount);
		}
		private bool WriteCountLine(byte startSimbol, int argsCount)
		{
			var part = argsCount.ToBytes();
			var length = ADDITIONAL_LINE_BYTES + part.Length;

			if (!HasSpace(length)) return false;

			_writingBuffer[_writingOffset++] = startSimbol;

			Array.Copy(part, 0, _writingBuffer, _writingOffset, part.Length);
			_writingOffset += part.Length;

			WriteNewLineToBuffer();

			return true;
		}
		private ArraySegment<byte> WriteData(ArraySegment<byte> data)
		{
			var bytesToWrite = _writingBuffer.Length - _writingOffset;

			if (bytesToWrite == 0)
				return data;

			if (data.Count < bytesToWrite)
				bytesToWrite = data.Count;

			Array.Copy(data.Array, data.Offset, _writingBuffer, _writingOffset, bytesToWrite);

			_writingOffset += bytesToWrite;

			return new ArraySegment<byte>(data.Array, data.Offset + bytesToWrite, data.Count - bytesToWrite);
		}

		private bool HasSpace(int dataLengthToWrite)
		{
			return _flushed == 0 && (_writingBuffer.Length) >= (_writingOffset + dataLengthToWrite);
		}
		private void WriteNewLineToBuffer()
		{
			_writingBuffer[_writingOffset++] = RedisConstants.NewLine[0];
			_writingBuffer[_writingOffset++] = RedisConstants.NewLine[1];
		}
		#endregion

		public int BytesInBuffer
		{
			get { return _writingOffset; }
		}
	}
}
