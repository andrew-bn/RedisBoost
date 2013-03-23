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

namespace NBoosters.RedisBoost.Core.Pipeline
{
	internal class RedisPipeline: IRedisPipeline
	{
		private readonly ConcurrentQueue<PipelineItem> _requestsQueue = new ConcurrentQueue<PipelineItem>();
		private readonly ConcurrentQueue<PipelineItem> _responsesQueue = new ConcurrentQueue<PipelineItem>(); 

		private readonly IRedisChannel _channel;
		
		private int _sendIsRunning = 0;
		private int _receiveIsRunning = 0;
		private int _pipelineIsClosed = 0;

		private volatile Exception _pipelineException = null;
		
		public RedisPipeline(IRedisChannel channel)
		{
			_channel = channel;
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
			PipelineItem item;
			
			ContinueSending:

			if (_requestsQueue.TryDequeue(out item))
			{ 
				if (_pipelineException==null)
					_channel.SendAsync(item.Request,ex=>ItemSendProcessDone(ex,item));
				else
				{
					item.CallBack(_pipelineException,null);
					goto ContinueSending;
				}
			}
			else if (!_channel.BufferIsEmpty && _pipelineException == null)
				_channel.Flush(BufferFlushProcessDone);
			else
			{
				Interlocked.Exchange(ref _sendIsRunning, 0);

				if (_requestsQueue.Count > 0 && Interlocked.CompareExchange(ref _sendIsRunning, 1, 0) == 0)
					goto ContinueSending;
			}
		}

		private void BufferFlushProcessDone(Exception exception)
		{
			if (exception!=null)
				_pipelineException = exception;

			RunSendProcess();
		}
		private void ItemSendProcessDone(Exception ex, PipelineItem item)
		{

			if (ex!=null)
				_pipelineException = ex;

			if (_pipelineException != null)
				item.CallBack(_pipelineException,null);
			else
			{
				_responsesQueue.Enqueue(item);
				TryRunReceiveProcess();
			}

			RunSendProcess();
		}

		private void TryRunReceiveProcess()
		{
			// start receive process if not yet started
			if (Interlocked.CompareExchange(ref _receiveIsRunning, 1, 0) == 0)
				RunReceiveProcess();
		}
		private void RunReceiveProcess()
		{
			PipelineItem item;

			ContinueReceiving:
			if (_responsesQueue.TryDequeue(out item))
			{
				if (_pipelineException == null)
					_channel.ReadResponseAsync((ex,r)=>ItemReceiveProcessDone(ex,r,item));
				else
				{
					item.CallBack(_pipelineException,null);
					goto ContinueReceiving;
				}
			}
			else
			{
				Interlocked.Exchange(ref _receiveIsRunning, 0);
				if (_responsesQueue.Count > 0 && Interlocked.CompareExchange(ref _receiveIsRunning, 1, 0) == 0)
					goto ContinueReceiving;
			}
		}

		private void ItemReceiveProcessDone(Exception ex, RedisResponse response, PipelineItem item)
		{
			if (ex!=null)
				_pipelineException = ex;

			if (_pipelineException != null)
				item.CallBack(_pipelineException, null);
			else
				item.CallBack(null, response);

			RunReceiveProcess();
		}

	
		public void ClosePipeline()
		{
			if (Interlocked.CompareExchange(ref _pipelineIsClosed, 1, 0) != 0)
				return;

			SpinWait.SpinUntil(() => _requestsQueue.Count == 0 && _responsesQueue.Count == 0);
		}
	}
}
