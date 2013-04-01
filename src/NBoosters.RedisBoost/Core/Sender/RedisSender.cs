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
using System.Globalization;
using System.Text;
using NBoosters.RedisBoost.Core.AsyncSocket;

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

		private readonly byte[] _buffer;
		private int _offset;
		private readonly IAsyncSocket _asyncSocket;
		private readonly bool _autoFlush;
		private readonly AsyncSocketEventArgs _flushArgs;
		private readonly SenderContext _senderContext;

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

			_buffer = new byte[bufferSize];
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

		public bool Flush(SenderAsyncEventArgs args)
		{
			_flushArgs.Error = null;
			_flushArgs.DataToSend = _buffer;
			_flushArgs.DataLength = _offset;
			_flushArgs.UserToken = args;

			var isAsync = _asyncSocket.Send(_flushArgs);
			if (!isAsync) FlushCallBack(false, _flushArgs);
			return isAsync;
		}

		private void FlushCallBack(AsyncSocketEventArgs args)
		{
			FlushCallBack(true, args);
		}

		private void FlushCallBack(bool async, AsyncSocketEventArgs args)
		{
			var senderArgs = (SenderAsyncEventArgs)args.UserToken;
			_offset = 0;
			args.UserToken = null;
			senderArgs.Error = args.Error;
			if (async) senderArgs.Completed(senderArgs);
		}


		public void EngageWith(ISocket socket)
		{
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
			var part = ConvertToByteArray(argsCount);
			var length = ADDITIONAL_LINE_BYTES + part.Length;

			if (!HasSpace(length)) return false;

			_buffer[_offset++] = startSimbol;

			Array.Copy(part, 0, _buffer, _offset, part.Length);
			_offset += part.Length;

			WriteNewLineToBuffer();

			return true;
		}
		private ArraySegment<byte> WriteData(ArraySegment<byte> data)
		{
			var bytesToWrite = _buffer.Length - _offset;

			if (bytesToWrite == 0)
				return data;

			if (data.Count < bytesToWrite)
				bytesToWrite = data.Count;

			Array.Copy(data.Array, data.Offset, _buffer, _offset, bytesToWrite);

			_offset += bytesToWrite;

			return new ArraySegment<byte>(data.Array, data.Offset + bytesToWrite, data.Count - bytesToWrite);
		}

		private bool HasSpace(int dataLengthToWrite)
		{
			return (_buffer.Length) >= (_offset + dataLengthToWrite);
		}
		private void WriteNewLineToBuffer()
		{
			_buffer[_offset++] = RedisConstants.NewLine[0];
			_buffer[_offset++] = RedisConstants.NewLine[1];
		}
		#endregion

		private byte[] ConvertToByteArray(int value)
		{
			return Encoding.UTF8.GetBytes(value.ToString(CultureInfo.InvariantCulture));
		}

		public int BytesInBuffer
		{
			get { return _offset; }
		}
	}
}
