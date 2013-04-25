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

		private byte[] _sendingBuffer;
		private byte[] _writingBuffer;
		private int _writingOffset;
		private int _sendingOffset;
		private readonly IAsyncSocket _asyncSocket;
		private readonly bool _autoFlush;
		private readonly AsyncSocketEventArgs _flushArgs;
		private readonly SenderContext _senderContext;
		private int _flushState;
		private Exception _socketException;
		const int NoFlush = 0;
		const int FlushIsRunning = 1;
		const int PendingFlush = 2;

		public RedisSender(IAsyncSocket asyncSocket, bool autoFlush)
			: this(asyncSocket, BUFFER_SIZE, autoFlush)
		{
			
		}

		public RedisSender(IAsyncSocket asyncSocket, int bufferSize, bool autoFlush)
		{
			_asyncSocket = asyncSocket;
			_autoFlush = autoFlush;
			_senderContext = new SenderContext();

			_flushArgs = new AsyncSocketEventArgs();
			_flushArgs.Completed = FlushCallBack;

			_sendingBuffer = new byte[bufferSize];
			_writingBuffer = new byte[bufferSize];
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
		public bool Flush(SenderAsyncEventArgs args)
		{
			SwapBuffers();

			_flushingArgs = args;
			var flushState = Interlocked.Increment(ref _flushState);

			if (flushState == PendingFlush)
				return true;
			
			if (flushState == FlushIsRunning)
			{
				DoFlushing(args);
				return false;
			}
			
			throw new InvalidOperationException("Invalid flush state");
		}

		private void SwapBuffers()
		{
			byte[] buffer = null;
			Interlocked.Exchange(ref buffer, _writingBuffer);
			Interlocked.Exchange(ref _writingBuffer, _sendingBuffer);
			Interlocked.Exchange(ref _sendingBuffer, buffer);

			Interlocked.Exchange(ref _sendingOffset, _writingOffset);
			Interlocked.Exchange(ref _writingOffset, 0);
		}

		private bool DoFlushing(SenderAsyncEventArgs args)
		{
			nextFlush:
			//_flushArgs.Error = _socketException;
			_flushArgs.DataToSend = _sendingBuffer;
			_flushArgs.DataLength = _sendingOffset;
			_flushArgs.UserToken = args;

			var isAsync = _asyncSocket.Send(_flushArgs);
			if (!isAsync)
			{
				FlushCallBack(false, _flushArgs);
				if (_flushState == FlushIsRunning)
					goto nextFlush;
			}
			return isAsync;
		}

		private void FlushCallBack(AsyncSocketEventArgs args)
		{
			FlushCallBack(true, args);
		}

		private void FlushCallBack(bool async, AsyncSocketEventArgs args)
		{
			// var senderArgs = (SenderAsyncEventArgs)args.UserToken;
			// _sendingOffset = 0;
			args.UserToken = null;

			Interlocked.Exchange(ref _socketException, args.Error);
			var flushState = Interlocked.Decrement(ref _flushState);

			if (flushState == FlushIsRunning) // flush was pending. call back should be called
			{
				if (async) DoFlushing(_flushingArgs);
				if (async) _flushingArgs.Completed(_flushingArgs);
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
			return (_writingBuffer.Length) >= (_writingOffset + dataLengthToWrite);
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
