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
using System.Threading.Tasks;
using NBoosters.RedisBoost.Core.Misk;
using NBoosters.RedisBoost.Core.Serialization;

namespace NBoosters.RedisBoost.Extensions.ManualResetEvent
{
	internal class RedisManualResetEvent : ExtensionBase, IManualResetEvent
	{
		static byte[] PULSE_MESSAGE = new byte[] { 1 };
		private readonly string _eventName;
		private readonly IPubSubClientsPool _pubSubPool;
		private volatile TaskCompletionSource<bool> _taskCompletion;
		private int _taskResultWasSet = 0;

		public RedisManualResetEvent(string eventName,
			IRedisClientsPool pool, IPubSubClientsPool pubSubPool,
			RedisConnectionStringBuilder connectionStringBuilder, BasicRedisSerializer serializer)
			: base(pool, connectionStringBuilder, serializer)
		{
			_eventName = eventName;
			_pubSubPool = pubSubPool;
		}

		public void Set()
		{
			ExecuteAction(() =>
				{
					using (var cli = GetClient())
					{
						cli.SetAsync(_eventName, 1).Wait();
						cli.PublishAsync(_eventName, PULSE_MESSAGE).Wait();
					}
				});
		}

		public void Reset()
		{
			ExecuteAction(() =>
				{
					using (var cli = GetClient())
					{
						cli.SetAsync(_eventName, 0).Wait();
					}
				});
		}

		public Task WaitOneAsync()
		{
			return WaitOneAsync(-1);
		}

		public Task<bool> WaitOneAsync(int millisecondsTimeout)
		{
			Interlocked.Exchange(ref _taskResultWasSet, 0);

			_taskCompletion = new TaskCompletionSource<bool>();
			StartTimer(millisecondsTimeout);
			var pubSub = GetPubSubClient();

			if (!OpenChannel(pubSub))
				return _taskCompletion.Task;

			bool isSignaledState;
			if (!TryCheckIsSignaledState(out isSignaledState))
				return _taskCompletion.Task;

			if (isSignaledState)
			{
				if (CloseChannel(pubSub))
					SetTaskResult(true);
			}
			else
			{
				pubSub.ReadMessageAsync()
					.ContinueWithIfNoError(_taskCompletion,
						t =>
						{
							if (t.Result.MessageType == ChannelMessageType.Message && CloseChannel(pubSub))
								SetTaskResult(true);
							else SetTaskResult(false);
						});
			}
			return _taskCompletion.Task;
		}
		private void SetTaskResult(Exception ex)
		{
			if (Interlocked.CompareExchange(ref _taskResultWasSet, 1, 0) == 0)
				_taskCompletion.SetException(ex);
		}
		private void SetTaskResult(bool result)
		{
			if (Interlocked.CompareExchange(ref _taskResultWasSet, 1, 0) == 0)
				_taskCompletion.SetResult(result);
		}
		private void StartTimer(int millisecondsTimeout)
		{
			if (millisecondsTimeout <= 0) return;

			Timer timer = null;
			timer = new Timer(s =>
				{
					timer.Dispose();
					SetTaskResult(false);
				});
		}

		private bool CloseChannel(IRedisSubscription subscription)
		{
			try
			{
				subscription.UnsubscribeAsync(_eventName).Wait();
				subscription.ReadMessageAsync().Wait();
				return true;
			}
			catch (Exception ex)
			{
				SetTaskResult(ex);
				return false;
			}
		}

		private bool OpenChannel(IRedisSubscription subscription)
		{
			try
			{
				subscription.SubscribeAsync(_eventName).Wait();
				subscription.ReadMessageAsync().Wait();
				return true;
			}
			catch (Exception ex)
			{
				SetTaskResult(ex.UnwrapAggregation());
				return false;
			}
		}

		private bool TryCheckIsSignaledState(out bool isSignaledState)
		{
			isSignaledState = false;
			try
			{
				using (var cli = GetClient())
				{
					var state = cli.GetAsync(_eventName).Result;
					isSignaledState = state.Value == null || state.As<int>() == 1; // signaled state
				}

				return true;
			}
			catch (Exception ex)
			{
				SetTaskResult(ex.UnwrapAggregation());
				return false;
			}

		}

		private IRedisSubscription GetPubSubClient()
		{
			return _pubSubPool.GetClient(ConnectionStringBuilder);
		}
	}
}
