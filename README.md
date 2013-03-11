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
* Now you can use IRedisClient to get an access to Redis api
