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
using System.Threading;
using NBoosters.RedisBoost.Core.RedisChannel;

namespace NBoosters.RedisBoost.Core.Pipeline
{
	internal class RedisPipeline: IRedisPipeline
	{
		private readonly ConcurrentQueue<PipelineItem> _requestsQueue = new ConcurrentQueue<PipelineItem>();
		private readonly ConcurrentQueue<PipelineItem> _responsesQueue = new ConcurrentQueue<PipelineItem>(); 

		private readonly IRedisChannel _channel;
		
		private int _sendIsRunning;
		private int _receiveIsRunning;
		private int _pipelineIsClosed;

		private volatile Exception _pipelineException;
		private readonly ChannelAsyncEventArgs _readChannelArgs = new ChannelAsyncEventArgs();
		private readonly ChannelAsyncEventArgs _sendChannelArgs = new ChannelAsyncEventArgs();
		private readonly ChannelAsyncEventArgs _flushChannelArgs = new ChannelAsyncEventArgs();
		private PipelineItem _currentReceiveItem;
		private PipelineItem _currentSendItem;
		public RedisPipeline(IRedisChannel channel)
		{
			_channel = channel;
			_readChannelArgs.Completed = ItemReceiveProcessDone;
			_sendChannelArgs.Completed = ItemSendProcessDone;
			_flushChannelArgs.Completed = BufferFlushProcessDone;
		}

		public void ExecuteCommandAsync(byte[][] args,Action<Exception, RedisResponse> callBack)
		{
			var item = new PipelineItem(args,callBack);
			if (_pipelineException != null)
				item.CallBack(_pipelineException, null);
			else if (_pipelineIsClosed != 0)
				item.CallBack(new RedisException("Pipeline is closed"), null);
			else
			{
				_requestsQueue.Enqueue(item);
				TryStartSendProcess();
			}
		}

		private void TryStartSendProcess()
		{
			if (Interlocked.CompareExchange(ref _sendIsRunning, 1, 0) == 0)
				RunSendProcess(); // start send process if not yet started
		}

		private void RunSendProcess()
		{
			while (true)
			{
				if (_requestsQueue.TryDequeue(out _currentSendItem))
				{
					if (_pipelineException == null)
					{
						_sendChannelArgs.SendData = _currentSendItem.Request;
						if (_channel.SendAsync(_sendChannelArgs)) return;
						ItemSendProcessDone(false, _sendChannelArgs);
					}
					else _currentSendItem.CallBack(_pipelineException, null);

				}
				else if (!_channel.BufferIsEmpty && _pipelineException == null)
				{
					if (_channel.Flush(_flushChannelArgs)) return;
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
		private void BufferFlushProcessDone(ChannelAsyncEventArgs args)
		{
			BufferFlushProcessDone(true,args);
		}
		private void BufferFlushProcessDone(bool async,ChannelAsyncEventArgs args)
		{
			_pipelineException = args.Exception;
			if (async) RunSendProcess();
		}
		private void ItemSendProcessDone(ChannelAsyncEventArgs args)
		{
			ItemSendProcessDone(true, args);
		}
		private void ItemSendProcessDone(bool async, ChannelAsyncEventArgs args)
		{
			_pipelineException = args.Exception;

			if (_pipelineException != null)
				_currentSendItem.CallBack(_pipelineException,null);
			else
			{
				_responsesQueue.Enqueue(_currentSendItem);
				TryRunReceiveProcess();
			}
			if (async) RunSendProcess();
		}

		private void TryRunReceiveProcess()
		{
			// start receive process if not yet started
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
						if (_channel.ReadResponseAsync(_readChannelArgs)) return;
						ItemReceiveProcessDone(false, _readChannelArgs);
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

		private void ItemReceiveProcessDone(ChannelAsyncEventArgs args)
		{
			ItemReceiveProcessDone(true,args);
		}

		private void ItemReceiveProcessDone(bool async,ChannelAsyncEventArgs args)
		{
			if (args.Exception != null)
				_pipelineException = args.Exception;

			if (_pipelineException != null)
				_currentReceiveItem.CallBack(_pipelineException, null);
			else
				_currentReceiveItem.CallBack(null, args.RedisResponse);

			if (async) RunReceiveProcess();
		}

	
		public void ClosePipeline()
		{
			if (Interlocked.CompareExchange(ref _pipelineIsClosed, 1, 0) != 0)
				return;

			SpinWait.SpinUntil(() => _requestsQueue.Count == 0 && _responsesQueue.Count == 0);
		}
	}
}
