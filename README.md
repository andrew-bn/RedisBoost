RedisBoost (.NET 4.5)
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

Connection string support
----------
You can describe your connections with connection strings.

```xml
	  <connectionStrings>
    		<add name="Redis" connectionString="data source=10.1.2.210:6379;initial catalog=7"/>
 	  </connectionStrings>
```

Now use this connection string to initialize new instance of RedisClient

```csharp
var cs = ConfigurationManager.ConnectionStrings["Redis"].ConnectionString;
var client = RedisClient.ConnectAsync(cs).Result;
```

The code above will make new connection to Redis server and perform a 'SELECT 7' command.

*Currently passing password in connection string is not supported, but will be soon*

Clients pool support
-----------
Each RedisClient instance represents one connection to Redis. 
If you create this instance directly (not using Clients pool)
then calling the IDisposable.Dispose will dispose socket and close connection to Redis.
If you are worried about the ammount of redis connections your application makes you can use clients pool

```csharp
[Test]
public void ClientsPool()
{
	using (var pool = RedisClient.CreateClientsPool())
	{
		IRedisClient cli1, cli2;
		using (cli1 = pool.CreateClientAsync(ConnectionString).Result)
		{
			cli1.SetAsync("Key", GetBytes("Value")).Wait();
		}
		using (cli2 = pool.CreateClientAsync(ConnectionString).Result)
		{
			cli2.GetAsync("Key").Wait();
		}
		Assert.AreEqual(cli1, cli2);
	}
}
```

Please consider the code above. cli1 and cli2 whould be the same instances, 
since IDisposable.Dispose won't close connection
but only will return client to pool.

Use RedisClient.CreateClientsPool() factory method to create clients pool. 
This method has some overloads so you can pass inactivity timeout and pool size.
Then inactivity timeout passes RedisClient is disconnected from Redis and disposed.

Keep in mind that if you switch RedisClient to Pub/Sub mode then this client can't be returned to pool.
So this client will be disposed if IDisposable.Dispose() is called.

Each instance of clients pool actually can manage many pools, one for each connection string.

If you dispose clients pool then all clients that are in pool will be disconnected from Redis and disposed.

Pipelining support
----------
Since Redis server supports pipelining, RedisBoost also supports it.

Any command that is sent to Redis is pipelined. 
Only Pub/Sub commands are sent directly to Redis and not pipelined. 
Then you switch to Pub/Sub mode Redis pipeline is closed.

Let's see an example

```csharp
[Test]
public void PipelineTest()
{
	using (var cli = CreateClient())
	{
		var tasks = new List<Task<byte[]>>();

		for (int i = 0; i < 10000; i++)
		{
			cli.SetAsync("Key" + i, GetBytes("Value"+i));
			tasks.Add(cli.GetAsync("Key"+i));
		}
		
		// some other work here...
		//...
		
		for (int i = 0; i < 10000; i++)
			Assert.AreEqual("Value"+i,GetString(tasks[i].Result));
	}
}
```
Please consider the example above. As you can see it executes 10000 commands, 
and does not wait until the end of execution.

At the end all responses are checked.

In the test library NBoosters.RedisBoost.Tests there is a 
IntegrationTests.PipelineTest_ParallelPipelining test that shows 
parallel work with the same redis client in pipeline style

Error handling
----------
* All Redis server errors are thrown RedisException and treated as not critical.
* All socket errors are wrapped into RedisException but treated as critical. Client would be disconnected and disposed.
