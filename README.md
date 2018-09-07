# akka-shards-spam-repro

The goal is to have an exact number of actors spread among the cluster, and to route messages to these actors in
a round robin/randomized manner. The actors number should remain constant, no matter how many nodes are running.

Proposed solution - something similar to HashCodeMessageExtractor, but each shard contains only one entity. Implemented extractor creates pairs of shardId/entityId like that:
/1/1
/2/2
...etc

# Repro

How to run:

Have some recent version of .Net Core installed.

```
> dotnet build
> dotnet run --whatever (this will cause it to be a seed node)
> dotnet run
```

And then type anything in one of the windows.

Expected result - some messages are printed, then nothing spectacular happens

Actual result - tons of messages like that:

```
[DEBUG][30-Aug-18 15:21:31][Thread 0009][LocalActorRefProvider(akka://repro)] Resolve of path sequence [/deadLetters] failed
[DEBUG][30-Aug-18 15:21:31][Thread 0026][[akka://repro/system/sharding/sharded#2139169847]] Forwarding request for shard [1] to [[akka.tcp://repro@127.0.0.1:4053/system/sharding/sharded#859126224]]
[DEBUG][30-Aug-18 15:21:31][Thread 0026][[akka://repro/system/sharding/sharded#2139169847]] Forwarding request for shard [1] to [[akka.tcp://repro@127.0.0.1:4053/system/sharding/sharded#859126224]]
[DEBUG][30-Aug-18 15:21:31][Thread 0009][LocalActorRefProvider(akka://repro)] Resolve of path sequence [/deadLetters] failed
[DEBUG][30-Aug-18 15:21:31][Thread 0026][[akka://repro/system/sharding/sharded#2139169847]] Forwarding request for shard [1] to [[akka.tcp://repro@127.0.0.1:4053/system/sharding/sharded#859126224]]
[DEBUG][30-Aug-18 15:21:31][Thread 0026][[akka://repro/system/sharding/sharded#2139169847]] Forwarding request for shard [1] to [[akka.tcp://repro@127.0.0.1:4053/system/sharding/sharded#859126224]]
[DEBUG][30-Aug-18 15:21:31][Thread 0011][LocalActorRefProvider(akka://repro)] Resolve of path sequence [/deadLetters] failed
[DEBUG][30-Aug-18 15:21:31][Thread 0026][[akka://repro/system/sharding/sharded#2139169847]] Forwarding request for shard [1] to [[akka.tcp://repro@127.0.0.1:4053/system/sharding/sharded#859126224]]
```

N.B. `remember-entities` is not used.

# UPDATE:

I just noticed that the problem does not show when you do a sequence of actions like that:

```
> dotnet build
> dotnet run --whatever (this will cause it to be a seed node)
type anything to send messages to sharded actors. All sharded entities will get spawned
> dotnet run
now it is possible to send messages on both nodes without issues by typing in terminals
```