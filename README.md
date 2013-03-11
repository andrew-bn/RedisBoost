RedisBoost
==========

* Thread-safe asynchronous architecture
* Supports pipelining that will tremendously boost your commands performance
* Tightly bound to Redis api. Does not expose high level abstractions
* Easy to use pub/sub api. Works directly on the level of Redis commands.
* Clients pool support
* Connection string support
 
Installation
----------
You can install RedisBoost via NuGet:

![Install-Pacakage NBoosters.RedisBoost](https://raw.github.com/NBooster/RedisBoost/master/images/nuget.png)

Let's get started
----------
* Include RedisBoost namespace

```csharp
  using NBoosters.RedisBoost;
```

* Make a connection to Redis.

```csharp
  IRedisClient client = RedisClient.ConnectAsync("127.0.0.1").Result;
```
* Now you can use IRedisClient to get an access to Redis api. To find out more about Redis visit http://redis.io/

Pub/Sub support
----------
To start working with Redis channels all you should do is to call IRedisClient.SubscribeAsync(params string[] channels) 
or IRedisClient.PSubscribeAsync(params string[] channels) method. 
These methods will return IRedisSubscription interface that allows you to subscribe or unsubscribe 
from the other channels and to receive messages from subscribed channels.
Keep in mind that you are working now not with high abstraction but on the lowest level. 
To read more about what messages could be sent to your channel visit http://redis.io/topics/pubsub page.
To receive these messages you can call IRedisSubscription.ReadMessageAsync() that will return message from channel.
There can be present not only messages that were published by other clients but also other types of messages, so you 
can arrange work with cannel and manage them the way you need. 
Also you can filter only messages you are intrested in.

Long story short, let's see an example:

```csharp
[Test]
public void Publish_Subscribe_WithFilter()
{
	using (var subscriber = CreateClient().SubscribeAsync("channel").Result)
	{
		using (var publisher = CreateClient())
		{
			publisher.PublishAsync("channel", GetBytes("Message")).Wait();

			var subResult = subscriber.ReadMessageAsync(ChannelMessageType.Message |
														ChannelMessageType.PMessage).Result;
					
			Assert.AreEqual(ChannelMessageType.Message, subResult.MessageType);
			Assert.AreEqual("channel", subResult.Channels[0]);
			Assert.AreEqual("Message", GetString(subResult.Value));
		}
	}
}
```

