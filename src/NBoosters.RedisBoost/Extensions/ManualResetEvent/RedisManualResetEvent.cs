using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NBoosters.RedisBoost.Core.Serialization;

namespace NBoosters.RedisBoost.Extensions.ManualResetEvent
{
	internal class RedisManualResetEvent:ExtensionBase, IManualResetEvent
	{
		static byte[] PULSE_MESSAGE = new byte[]{1};
		private readonly string _eventName;
		private readonly IRedisClientsPool _pubSubPool;

		public RedisManualResetEvent(string eventName, 
			IRedisClientsPool pool, IRedisClientsPool pubSubPool, 
			RedisConnectionStringBuilder connectionStringBuilder, BasicRedisSerializer serializer)
			:base(pool,connectionStringBuilder,serializer)
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

		public System.Threading.Tasks.Task WaitOneAsync()
		{
			return WaitOneAsync(-1);
		}

		public System.Threading.Tasks.Task<bool> WaitOneAsync(int millisecondsTimeout)
		{
			var pubSub = GetPubSubClient();
			pubSub.SubscribeAsync(_eventName).Wait();
			pubSub.ReadMessageAsync().Wait(); // read publish notification

			while (true)
			{
				if (IsSignaledState())
				{
					pubSub.UnsubscribeAsync(_eventName).Wait();
					pubSub.ReadMessageAsync().Wait(); // read unsubscribe notification
					return null; // return true here
				}

				// wait for pulse
				if (!pubSub.ReadMessageAsync(ChannelMessageType.Message).Wait(millisecondsTimeout))
				{
					pubSub.UnsubscribeAsync(_eventName).Wait();
					pubSub.ReadMessageAsync().Wait(); // read unsubscribe notification
					return null; // return false here - timeout occured
				}
			}
		}

		private bool IsSignaledState()
		{
			return ExecuteFunc(() =>
				{
					using (var cli = GetClient())
					{
						var state = cli.GetAsync(_eventName).Result;
						return state.Value == null || state.As<int>() == 1; // signaled state
					}
				});
		}

		private IRedisSubscription GetPubSubClient()
		{
			return null;
		}
	}
}
