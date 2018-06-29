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
using System.Net;
using System.Threading;
using RedisBoost.Core.AsyncSocket;
using RedisBoost.Core.Receiver;
using RedisBoost.Core.Sender;
using RedisBoost.Core.Serialization;

namespace RedisBoost.Core.Pipeline
{
	internal class RedisPipeline: IRedisPipeline
	{
		private const int MaxQueueSize = 800;

		private readonly IAsyncSocket _asyncSocket;
		private readonly IRedisSender _redisSender;
		private readonly IRedisReceiver _redisReceiver;
		private ISocket _socket;
		private ConcurrentQueue<PipelineItem> _requestsQueue = new ConcurrentQueue<PipelineItem>();
		private ConcurrentQueue<PipelineItem> _responsesQueue = new ConcurrentQueue<PipelineItem>(); 

		private int _sendIsRunning;
		private int _receiveIsRunning;
		private int _pipelineIsInOneWayMode;

		private volatile Exception _pipelineException;
		private readonly ReceiverAsyncEventArgs _readArgs = new ReceiverAsyncEventArgs();
		private readonly SenderAsyncEventArgs _sendArgs = new SenderAsyncEventArgs();
		private readonly SenderAsyncEventArgs _flushChannelArgs = new SenderAsyncEventArgs();
		private readonly AsyncSocketEventArgs _notIoArgs = new AsyncSocketEventArgs();

		private PipelineItem _currentReceiveItem;
		private PipelineItem _currentSendItem;

		public RedisPipeline(IAsyncSocket asyncSocket, IRedisSender redisSender, IRedisReceiver redisReceiver)
		{
			_asyncSocket = asyncSocket;
			_redisSender = redisSender;
			_redisReceiver = redisReceiver;
			_readArgs.Completed = ItemReceiveProcessDone;
			_sendArgs.Completed = ItemSendProcessDone;
			_flushChannelArgs.Completed = BufferFlushProcessDone;
		}

		public void SendRequestAsync(byte[][] args, Action<Exception, RedisResponse> callBack)
		{
			ExecuteCommandAsync(args, new PipelineItem(args, callBack,true));
		}

		public void ExecuteCommandAsync(byte[][] args, Action<Exception, RedisResponse> callBack)
		{
			ExecuteCommandAsync(args, new PipelineItem(args, callBack,false));
		}

		public void ExecuteCommandAsync(byte[][] args,PipelineItem item)
		{
			if (_requestsQueue.Count > MaxQueueSize)
				SpinWait.SpinUntil(() => _requestsQueue.Count < MaxQueueSize);

			if (_pipelineException != null)
				item.CallBack(_pipelineException, null);
			else if (_pipelineIsInOneWayMode != 0 && !item.IsOneWay)
				item.CallBack(new RedisException("Pipeline is in OneWay mode"), null);
			else
			{
				_requestsQueue.Enqueue(item);
				TryStartSendProcess();
			}
		}

		private void TryStartSendProcess()
		{
			if (Interlocked.CompareExchange(ref _sendIsRunning, 1, 0) == 0)
				RunSendProcess();
		}

		private void RunSendProcess()
		{
			while (true)
			{
				if (_requestsQueue.TryDequeue(out _currentSendItem))
				{
					if (_pipelineException == null)
					{
						_sendArgs.DataToSend = _currentSendItem.Request;
						if (_redisSender.Send(_sendArgs)) return;
						ItemSendProcessDone(false, _sendArgs);
					}
					else _currentSendItem.CallBack(_pipelineException, null);

				}
				else if (_redisSender.BytesInBuffer>0 && _pipelineException == null)
				{
					if (_redisSender.Flush(_flushChannelArgs)) return;
					BufferFlushProcessDone(false,_flushChannelArgs);
				}
				else
				{
					Interlocked.Exchange(ref _sendIsRunning, 0);

					if (_requestsQueue.Count <= 0 || Interlocked.CompareExchange(ref _sendIsRunning, 1, 0) != 0)
						return;
				}
			}
		}
		private void BufferFlushProcessDone(SenderAsyncEventArgs args)
		{
			BufferFlushProcessDone(true,args);
		}
		private void BufferFlushProcessDone(bool async, SenderAsyncEventArgs args)
		{
			_pipelineException = args.Error;
			if (async) RunSendProcess();
		}
		private void ItemSendProcessDone(SenderAsyncEventArgs args)
		{
			ItemSendProcessDone(true, args);
		}
		private void ItemSendProcessDone(bool async, SenderAsyncEventArgs args)
		{
			_pipelineException = args.Error;

			if (_pipelineException != null)
				_currentSendItem.CallBack(_pipelineException, null);
			else if (!_currentSendItem.IsOneWay)
			{
				_responsesQueue.Enqueue(_currentSendItem);
				TryRunReceiveProcess();
			}
			else _currentSendItem.CallBack(null, null);

			if (async) RunSendProcess();
		}
		public void ReadResponseAsync(Action<Exception, RedisResponse> callBack)
		{
			var item = new PipelineItem(callBack);
			_responsesQueue.Enqueue(item);
			TryRunReceiveProcess();
		}
		private void TryRunReceiveProcess()
		{
			if (Interlocked.CompareExchange(ref _receiveIsRunning, 1, 0) == 0)
				RunReceiveProcess();
		}
		private void RunReceiveProcess()
		{
			while (true)
			{
				if (_responsesQueue.TryDequeue(out _currentReceiveItem))
				{
					if (_pipelineException == null)
					{
						if (_redisReceiver.Receive(_readArgs)) return;
						ItemReceiveProcessDone(false, _readArgs);
					}
					else _currentReceiveItem.CallBack(_pipelineException, null);
				}
				else
				{
					Interlocked.Exchange(ref _receiveIsRunning, 0);
					if (_responsesQueue.Count == 0 || Interlocked.CompareExchange(ref _receiveIsRunning, 1, 0) != 0)
						return;
				}
			}
		}

		private void ItemReceiveProcessDone(ReceiverAsyncEventArgs args)
		{
			ItemReceiveProcessDone(true,args);
		}

		private void ItemReceiveProcessDone(bool async, ReceiverAsyncEventArgs args)
		{
			_pipelineException = args.Error;

			if (_pipelineException != null)
				_currentReceiveItem.CallBack(_pipelineException, null);
			else
				_currentReceiveItem.CallBack(null, args.Response);

			if (async) RunReceiveProcess();
		}

	
		public void OneWayMode()
		{
			if (Interlocked.CompareExchange(ref _pipelineIsInOneWayMode, 1, 0) != 0)
				return;

			SpinWait.SpinUntil(() => _requestsQueue.Count == 0 && _responsesQueue.Count == 0);
		}

		public void EngageWith(ISocket socket)
		{
			_socket = socket;
			_asyncSocket.EngageWith(socket);
			_redisReceiver.EngageWith(socket);
			_redisSender.EngageWith(socket);
		}

		public void EngageWith(IRedisSerializer serializer)
		{
			_redisReceiver.EngageWith(serializer);
		}

		public void OpenConnection(EndPoint endPoint, Action<Exception> callBack)
		{
			_notIoArgs.Completed = a => callBack(a.Error);
			_notIoArgs.RemoteEndPoint = endPoint;
			if (!_asyncSocket.Connect(_notIoArgs))
				callBack(_notIoArgs.Error);
		}

		public void CloseConnection(Action<Exception> callBack)
		{
			_notIoArgs.Completed = a => callBack(a.Error);
			if (!_asyncSocket.Disconnect(_notIoArgs)) 
				callBack(_notIoArgs.Error);
		}

		public void DisposeAndReuse()
		{
			_socket.Dispose();
		}

		public void ResetState()
		{
			_sendIsRunning = 0;
			_receiveIsRunning = 0;
			_pipelineIsInOneWayMode = 0;
			_requestsQueue = new ConcurrentQueue<PipelineItem>();
			_responsesQueue = new ConcurrentQueue<PipelineItem>();
			_pipelineException = null;
			_notIoArgs.Error = null;
			_sendArgs.Error = null;
			_readArgs.Error = null;
			_flushChannelArgs.Error = null;
		}
	}
}
