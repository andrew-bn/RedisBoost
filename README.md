RedisBoost (.NET 4.0)
==========

* [Redis 2.6 commands set](http://redis.io/commands) support
* [Serialization](#serialization) support
* Thread-safe asynchronous architecture
* Supports [pipelining](#pipelining-support) that will tremendously boost your commands performance
* Tightly bound to Redis api. Does not expose high level abstractions
* Easy to use [pub/sub api](#pubsub-support). Works directly on the level of Redis commands.
* [Clients pool](#clients-pool-support) support
* [Connection string] (#connection-string-support) support
 
Installation
----------
You can install RedisBoost via NuGet:

![Install-Pacakage NBoosters.RedisBoost](https://raw.github.com/andrew-bn/RedisBoost/master/images/nuget.png)

Let's get started
----------
* Include RedisBoost namespace

```csharp
  using NBoosters.RedisBoost;
```

* Make a connection to Redis.

```csharp
  IRedisClient client = await RedisClient.ConnectAsync("127.0.0.1", 6379);
```
* Now you can use IRedisClient to get an access to Redis api. To find out more about Redis visit http://redis.io/

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

Serialization
----------
**Redis data serializer**

Each instance of RedisClient has a reference to serializer.
As a default serializer RedisBoost uses RedisClient.DefaultSerialzier. 

There is a default implementation that uses 
[System.Runtime.Serialization.DataContractSerializer](http://msdn.microsoft.com/en-us/library/system.runtime.serialization.datacontractserializer.aspx)
to serialze complex objects.
Primitive types, such as numeric types, strings, Guid, DateTime are first represented as strings 
and then serialized to byte[].

Also you can create your own implementation of serializer. 
All you need is to create a class with BasicRedisSerializer as a parent and override some methods. 
There will be an example soon.

```csharp
// creates redis client with default implementation of serializer (RedisClient.DefaultSerialzier)
var redisClient = await RedisClient.ConnectAsync("127.0.0.1",6379);

// you can setup your own serializer as a default
RedisClient.DefaultSerialzier = mySerializer;
var redisClient = await RedisClient.ConnectAsync("127.0.0.1",6379); // this instance will use mySerializer

// or you can pass serializer while creating redis client
var redisClient = await RedisClient.ConnectAsync("127.0.0.1",6379,serializer: mySerialzier); // this instance will use mySerializer
```

**Serialization of commands parameters**

Redis commands that demands data to be passed as command parameter has several overloads in RedisClient.
* First overload takes byte[] as a parameter
* Second overload takes generic parameter, that will be serialized to byte[]

```csharp
cli.SetAsync("Key", new byte[]{1,2,3}).Wait();
cli.SetAsync("Key2", new MyObject()).Wait(); // second parameter will be serialized to byte array
```

**Bulk and MultiBulk responses**

Some Redis commands return bulk or multi-bulk responses (http://redis.io/topics/protocol). 
In this case RedisBoost returns instanses of Bulk or MultiBulk classes.

Bulk could be implicitly converted to byte[]. MultiBulk could be implicitly converted to byte[][]. 
Both classes have IsNull property to check whether response is bulk (multi-bulk) null. 
If you try to implicitly convert null response to byte[] or byte[][] null will be returned, and any other operation
would generate RedisException.

Also any Redis response could be deserialized to the type you want.

```csharp
// bulk reply examples
byte[] result = cli.GetAsync("Key").Result; //implicit conversion to byte[]
string result = cli.GetAsync("Key").Result.As<string>(); //deserialization to specified type

// multi-bulk reply examples
byte[][] result = cli.MGetAsync("Key", "Key2").Result; //implicit conversion to byte[][];
string[] result = cli.MGetAsync("Key", "Key2").Result.AsArray<string>(); //deserialization to array of specified types

// each part of multi-bulk reply could be treated separately
var multiBulk = cli.MGetAsync("Key", "Key2").Result;
byte[] firstPart = multiBulk[0];//implicit conversion to byte[]
var secondPart = multiBulk[1].As<MyObject>();//deserialization to specified type
```

Pub/Sub support
----------
To start working with Redis channels all you should do is to call IRedisClient.SubscribeAsync(params string[] channels) 
or IRedisClient.PSubscribeAsync(params string[] channels) method. 
These methods will return IRedisSubscription interface that allows you to subscribe or unsubscribe 
from the other channels and to receive messages from subscribed channels. 

To receive messages you can call IRedisSubscription.ReadMessageAsync() that will return message from channel.
Keep in mind that you are working now not with high abstraction,
so from channel could be read not only messages that were published by other clients but also other types 
of messages, so you can arrange work with cannel and manage them the way you need. 
To read more about what messages could be sent to your channel visit http://redis.io/topics/pubsub page.
Also there can be different strategies to receive and process channel massages and because of IRedisSubscription.ReadMessageAsync()
returns Task object you can easily organize Event-based Asynchronous Pattern or use async/await feature.

ReadMessageAsync has a useful overload which allows you to filter messages you are interested in.

Long story short, let's see an example:

```csharp
[Test]
public void Publish_Subscribe_WithFilter()
{
	using (var subscriber = CreateClient().SubscribeAsync("channel").Result)
	{
		using (var publisher = CreateClient())
		{
			publisher.PublishAsync("channel", "Some message").Wait();
			var channelMessage = subscriber.ReadMessageAsync(ChannelMessageType.Message | 
									 ChannelMessageType.PMessage).Result;
			Assert.AreEqual(ChannelMessageType.Message, channelMessage.MessageType);
			Assert.AreEqual("channel", channelMessage.Channels[0]);
			Assert.AreEqual("Some message", channelMessage.Value.As<string>());
		}
	}
}
```

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
			cli1.SetAsync("Key", "Value").Wait();
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

If RedisClient was disconnected, disposed, was called QUIT command or you've switched to Pub/Sub mode then this
client can't be returned to pool. Such clients would be disposed if IDisposable.Dispose() is called.

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
		var tasks = new List<Task<Bulk>>()
		for (int i = 0; i < 10000; i++)
		{
			cli.SetAsync("Key" + i, "Value" + i);
			tasks.Add(cli.GetAsync("Key" + i));
		}
		// some other work here...
		//...
		for (int i = 0; i < 10000; i++)
			Assert.AreEqual("Value" + i, tasks[i].Result.As<string>());
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
* All Redis server errors are thrown as RedisException and treated as not critical.
* All socket errors are wrapped into RedisException but treated as critical. Client would be disconnected and disposed.
