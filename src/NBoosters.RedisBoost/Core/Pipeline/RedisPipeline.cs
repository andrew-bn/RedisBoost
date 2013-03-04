using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

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

		public Task<RedisResponse> ExecuteCommandAsync(params byte[][] args)
		{
			var item = new PipelineItem(args, new TaskCompletionSource<RedisResponse>());

			if (_pipelineException != null)
				item.TaskCompletionSource.SetException(_pipelineException);
			else if (_pipelineIsClosed != 0)
				item.TaskCompletionSource.SetException(new RedisException("Pipeline is closed"));
			else
			{
				_requestsQueue.Enqueue(item);
				TryStartSendProcess();
			}

			return item.TaskCompletionSource.Task;
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
				_channel.SendAsync(item.Request).ContinueWith(ItemSendProcessDone, item);
			else if (!_channel.BufferIsEmpty)
				_channel.Flush().ContinueWith(BufferFlushProcessDont);
			else
			{
				Interlocked.Exchange(ref _sendIsRunning, 0);

				if (_requestsQueue.Count > 0 && Interlocked.CompareExchange(ref _sendIsRunning, 1, 0) == 0)
					goto ContinueSending;
			}
		}

		private void BufferFlushProcessDont(Task task)
		{
			if (task.IsFaulted)
				_pipelineException = task.Exception;

			RunSendProcess();
		}
		private void ItemSendProcessDone(Task task, object state)
		{
			var item = (PipelineItem)state;

			if (task.IsFaulted)
				_pipelineException = task.Exception;

			if (_pipelineException != null)
			{
				item.TaskCompletionSource.SetException(_pipelineException);
				return;
			}

			_responsesQueue.Enqueue(item);
			TryRunReceiveProcess();

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
				_channel.ReadResponseAsync().ContinueWith(ItemReceiveProcessDone, item);
				return;
			}

			Interlocked.Exchange(ref _receiveIsRunning, 0);
			if (_responsesQueue.Count > 0 && Interlocked.CompareExchange(ref _receiveIsRunning, 1, 0) == 0)
				goto ContinueReceiving;

		}

		private void ItemReceiveProcessDone(Task<RedisResponse> task, object state)
		{
			var item = (PipelineItem)state;

			if (task.IsFaulted)
				_pipelineException = task.Exception;

			if (_pipelineException != null)
			{
				item.TaskCompletionSource.SetException(_pipelineException);
				return;
			}

			item.TaskCompletionSource.SetResult(task.Result);

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
