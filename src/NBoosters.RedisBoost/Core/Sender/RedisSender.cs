using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NBoosters.RedisBoost.Core.AsyncSocket;

namespace NBoosters.RedisBoost.Core.Sender
{
	internal class RedisSender: IRedisSender
	{
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
		private readonly AsyncSocketEventArgs _flushArgs;
		private SenderContext _senderContext;

		public RedisSender(IAsyncSocket asyncSocket)
		{
			_asyncSocket = asyncSocket;
			_senderContext = new SenderContext();

			_flushArgs = new AsyncSocketEventArgs();
			_flushArgs.Completed = FlushCallBack;

			_buffer = new byte[BUFFER_SIZE];
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
			var context = (SenderContext) args.UserToken;

			if (context.EventArgs.HasError)
				return CallOnSendCompleted(async, context);

			if (context.SendState == 0)
			{
				if (!WriteArgumentsCountLine(context.EventArgs.DataToSend.Length))
					return Flush(context.EventArgs) || SendDataTask(async, args);

				context.SendState = 1;
			}

			if (context.SendState > 0)
			{
				for (; context.PartIndex < context.EventArgs.DataToSend.Length; context.PartIndex++)
				{
					if (context.SendState == 1)
					{
						if (!WriteDataSizeLine(context.EventArgs.DataToSend[context.PartIndex].Length))
							return Flush(context.EventArgs) || SendDataTask(async, args);

						context.SendState = 2;
						context.ArraySegment = 
							new ArraySegment<byte>(context.EventArgs.DataToSend[context.PartIndex],
													0,
													context.EventArgs.DataToSend[context.PartIndex].Length);
					}

					if (context.SendState == 2)
					{
						while (true)
						{
							context.ArraySegment = WriteData(context.ArraySegment);
							if (context.ArraySegment.Count > 0)
								return Flush(context.EventArgs) || SendDataTask(async, args);

							context.SendState = 3;
							break;
						}
					}
					if (context.SendState == 3)
					{
						if (!WriteNewLine())
							return Flush(context.EventArgs) || SendDataTask(async, args);

						context.SendState = 1;
					}
				}
			}
			return CallOnSendCompleted(async, context);
		}

		private bool CallOnSendCompleted(bool async, SenderContext context)
		{
			if (async) context.CallBack(context.EventArgs);
			return async;
		}

		public bool Flush(SenderAsyncEventArgs args)
		{
			_flushArgs.DataToSend = _buffer;
			_flushArgs.DataLength = _offset;
			_flushArgs.UserToken = args;

			var isAsync = _asyncSocket.Send(_flushArgs);
			if (!isAsync) FlushCallBack(false, _flushArgs);
			return isAsync;
		}
		
		private void FlushCallBack(AsyncSocketEventArgs args)
		{
			FlushCallBack(true,args);
		}

		private void FlushCallBack(bool async, AsyncSocketEventArgs args)
		{
			var senderArgs = (SenderAsyncEventArgs)args.UserToken;
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
			var length = 3 + part.Length;

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
	}
}
