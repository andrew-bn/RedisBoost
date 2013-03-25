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
using NBoosters.RedisBoost.Core;
using NBoosters.RedisBoost.Core.Serialization;
using NBoosters.RedisBoost.Misk;

namespace NBoosters.RedisBoost.Extensions.ManualResetEvent
{
	internal class RedisManualResetEvent : ExtensionBase, IManualResetEvent
	{
		private static readonly byte[] PULSE_MESSAGE = new byte[] { 1 };
		private readonly string _eventName;
		private readonly IPubSubClientsPool _pubSubPool;
		private TaskCompletionSource<bool> _taskCompletion;
		private int _taskResultWasSet = 0;
		private int _wasUnsubscribed = 0;
		private int _processingResult = 0;
		private Timer _timer;
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

		public Task<bool> WaitOneAsync()
		{
			return WaitOneAsync(-1);
		}

		private bool TryHoldWaitBlock(out TaskCompletionSource<bool> result)
		{
			result = null;
			while (true)
			{
				Interlocked.Exchange(ref result, _taskCompletion);

				if (result != null) return true;

				result = new TaskCompletionSource<bool>();
				if (Interlocked.CompareExchange(ref _taskCompletion, result, null) == null)
					return true;
			}
		}
		private void LeaveWaitBlock(IRedisSubscription pubSub)
		{
			Interlocked.Exchange(ref _taskCompletion, null);
			pubSub.Dispose();
		}
		public Task<bool> WaitOneAsync(int millisecondsTimeout)
		{
			TaskCompletionSource<bool> tcs;
			if (!TryHoldWaitBlock(out tcs)) return tcs.Task;

			Interlocked.Exchange(ref _taskResultWasSet, 0);
			Interlocked.Exchange(ref _wasUnsubscribed, 0);
			Interlocked.Exchange(ref _processingResult, 0);

			var pubSub = GetPubSubClient();
			StartTimer(millisecondsTimeout, pubSub);
			if (!OpenChannel(pubSub))
			{
				LeaveWaitBlock(pubSub);
				return tcs.Task;
			}

			bool isSignaledState;
			if (!TryCheckIsSignaledState(out isSignaledState))
			{
				LeaveWaitBlock(pubSub);
				return tcs.Task;
			}

			if (isSignaledState)
			{
				if (CloseChannel(pubSub))
					SetTaskResult(true);

				LeaveWaitBlock(pubSub);
			}
			else
			{
				pubSub.ReadMessageAsync()
					.ContinueWith(
						t =>
						{
							var timeout = 0 != Interlocked.CompareExchange(ref _processingResult, 1, 0);
							if (!timeout)
							{
								if (t.IsFaulted)
									SetTaskResult(t.Exception);
								else if (t.Result.MessageType == ChannelMessageType.Message && CloseChannel(pubSub))
									SetTaskResult(true);
								else SetTaskResult(false);
							}
							LeaveWaitBlock(pubSub);
						});
			}
			return tcs.Task;
		}
		private void StartTimer(int millisecondsTimeout, IRedisSubscription subscription)
		{
			if (millisecondsTimeout <= 0) return;
			_timer = new Timer(s =>
			{
				if (Interlocked.CompareExchange(ref _processingResult, 1, 0) == 0)
				{
					_timer.Dispose();
					UnsubscribeFromChannel(subscription);
					SetTaskResult(false);
				}
			});
		}
		private void SetTaskResult(Exception ex)
		{
			if (Interlocked.CompareExchange(ref _taskResultWasSet, 1, 0) == 0)
				_taskCompletion.SetException(ex.UnwrapAggregation());
		}

		private void SetTaskResult(bool result)
		{
			if (Interlocked.CompareExchange(ref _taskResultWasSet, 1, 0) == 0)
				_taskCompletion.SetResult(result);
		}

		private bool UnsubscribeFromChannel(IRedisSubscription subscription)
		{
			try
			{
				subscription.UnsubscribeAsync(_eventName).Wait();
				return true;
			}
			catch (Exception ex)
			{
				SetTaskResult(ex);
				return false;
			}
		}
		private bool CloseChannel(IRedisSubscription subscription)
		{
			try
			{
				if (!UnsubscribeFromChannel(subscription))
					return false;
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
