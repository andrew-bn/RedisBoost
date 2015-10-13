Release Notes
==============
1.6.4
--------------
- Interface for HyperLogLog commands
- Interface for command SlaveOf NO ONE

1.6.3
--------------
- Exception processing logic changed: ReadMessageAsync and connection failure #4. Now ReadMessageAsync() raises RedisException if some socket issue happened, returns Unknown message if the format of received message is not valid. ReadMessageAsync(filter) raises RedisException if some socket issue happened (inner exception will be preserved), raises RedisException if Unknown packed was received

1.6.2
--------------
- Bug fix: Null reference exception #3. Reason - error in empty Bulk responce parsing
- ExecuteAsync command is open to external usage

1.6.1
--------------
- xml documentation added to nuget package

1.6.0
--------------
- solution was renamed to RedisBoost. NuGet package was renamed to RedisBoost. 
To install, run Install-Package RedisBoost.
- [PUBSUB command](http://redis.io/commands/pubsub) support.

1.5.8
--------------
- new pipeline strategy introduced. RedisBoost became faster, more reliable and robust. 
[Benchmark tests](https://github.com/andrew-bn/RedisBoost/wiki/Benchmark) were provided for this version of RedisBoost.
- minor bug fixes

1.5.7
--------------
- max pipeline size was introduced. Now pipeline can not be oversized, 
so the commands sequence of any size can be processed by redisboost without memory penalties.

1.5.6
--------------
- pipeline was redesigned. Now it is more robust and reliable.
- Pub/Sub commands now are executed through pipeline environment which is thread safe and fast. 
So, now concurrent pub/sub commands could be sent.

1.5.5
--------------
- fixed pipeline issue with big amount of commands: pipeline failed to process all pipelined commands 
(e.g. more than 10000 commands in pipeline)
- clarified pubsub ReadMessageAsync command. If command is called without filter, then all errors and unknown packets are 
returned to client without exceptions thrown. If ReadMessageAsync is called with filter, then exception is thrown if
error occured or unknown packet was received. So ReadMessageAsync without filter is guaranteed to be exceptionless.
- minor fixes and redesign

1.5.4
--------------
- project moved to .NET40
- updates accumulated since 1.3.5
- minor fixes

1.3.5
--------------
- serialization capability added
- pipelining support added
- MOVE, OBJECT, SORT, BITOP, SETBIT, GETBIT, MIGRATE, HINCRBYFLOAT, INCRBYFLOAT commands support added
- Supports whole set of Redis 2.6 commands (only some Server commands are eliminated)

1.1.2
--------------
- clients pool support added

1.1.0
--------------
- initial version. Supports almost whole Redis 2.6 command set.



