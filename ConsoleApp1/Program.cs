using System;
using System.IO;
using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Configuration;
using Akka.Event;

namespace ConsoleApp1
{
    class Program
    {
        /// <summary>
        /// The goal is to have an exact number of actors spread among the cluster, and to route messages to these actors in
        /// a round robin/randomized manner. The actors number should remain constant, no matter how many nodes are running.
        ///
        /// Proposed solution - something similar to HashCodeMessageExtractor, but each shard contains only one entity.
        ///
        /// It seems like both below implementations of IMessageExtractor extractor should give the above behavior.
        ///
        /// How to run:
        ///
        /// Have some recent version of .Net Core installed.
        ///  
        /// > dotnet build
        /// > dotnet run --whatever (this will cause it to be a seed node)
        /// > dotnet run
        ///
        /// And then type anything in one of the windows.
        ///
        /// Expected result - some messages are printed, then nothing spectacular happens
        ///
        /// Actual result - tons of messages like that:
        ///
        /// [DEBUG][30-Aug-18 15:21:31][Thread 0009][LocalActorRefProvider(akka://repro)] Resolve of path sequence [/deadLetters] failed
        /// [DEBUG][30-Aug-18 15:21:31][Thread 0026][[akka://repro/system/sharding/sharded#2139169847]] Forwarding request for shard [1] to [[akka.tcp://repro@127.0.0.1:4053/system/sharding/sharded#859126224]]
        /// [DEBUG][30-Aug-18 15:21:31][Thread 0026][[akka://repro/system/sharding/sharded#2139169847]] Forwarding request for shard [1] to [[akka.tcp://repro@127.0.0.1:4053/system/sharding/sharded#859126224]]
        /// [DEBUG][30-Aug-18 15:21:31][Thread 0009][LocalActorRefProvider(akka://repro)] Resolve of path sequence [/deadLetters] failed
        /// [DEBUG][30-Aug-18 15:21:31][Thread 0026][[akka://repro/system/sharding/sharded#2139169847]] Forwarding request for shard [1] to [[akka.tcp://repro@127.0.0.1:4053/system/sharding/sharded#859126224]]
        /// [DEBUG][30-Aug-18 15:21:31][Thread 0026][[akka://repro/system/sharding/sharded#2139169847]] Forwarding request for shard [1] to [[akka.tcp://repro@127.0.0.1:4053/system/sharding/sharded#859126224]]
        /// [DEBUG][30-Aug-18 15:21:31][Thread 0011][LocalActorRefProvider(akka://repro)] Resolve of path sequence [/deadLetters] failed
        /// [DEBUG][30-Aug-18 15:21:31][Thread 0026][[akka://repro/system/sharding/sharded#2139169847]] Forwarding request for shard [1] to [[akka.tcp://repro@127.0.0.1:4053/system/sharding/sharded#859126224]]
        /// 
        /// </summary>
        
        class MyExtractor<T> : IMessageExtractor
        {
            public readonly int MaxNumberOfShards;
            
            public MyExtractor(int maxNumberOfShards)
            {
                MaxNumberOfShards = maxNumberOfShards;
            }

            public string EntityId(object message)
            {
                string id;
                if (message is ShardRegion.StartEntity se)
                {
                    id = se.EntityId;
                }
                else if (message is T msg)
                {
                    id = "the_one_and_only";
                }
                else
                {
                    id = null;
                }
                return id;
            }

            public object EntityMessage(object message)
            {
                return message;
            }

            public string ShardId(object message)
            {
                string id;
                if (message is T msg)
                {
                    var h = msg.GetHashCode();
                    id = (Math.Abs(h) % MaxNumberOfShards).ToString();
                }
                else
                {
                    id = "0";
                }
                return id;
            }
        }
        
        // Another implementation that also doesn't work
        class MyExtractor2<T> : IMessageExtractor
        {
            public readonly int MaxNumberOfShards;
            
            public MyExtractor2(int maxNumberOfShards)
            {
                MaxNumberOfShards = maxNumberOfShards;
            }

            public string EntityId(object message)
            {
                string id;
                if (message is ShardRegion.StartEntity se)
                {
                    id = se.EntityId;
                }
                else if (message is T msg)
                {
                    var h = msg.GetHashCode();
                    id = (Math.Abs(h) % MaxNumberOfShards).ToString();
                }
                else
                {
                    id = null;
                }

                return id;
            }

            public object EntityMessage(object message)
            {
                return message;
            }

            public string ShardId(object message)
            {
                return EntityId(message);
            }
        }
        
        public class MyActor: ReceiveActor
        {
            private readonly ILoggingAdapter log = Context.GetLogger();

            public MyActor()
            {
                Receive<string>(message => {
                    log.Info("Received String message: {0}", message);
                });
            }
        }

        
        static void Main(string[] args)
        {
            var config =
                args.Length > 0
                    ? ConfigurationFactory.ParseString(File.ReadAllText("config-lh.hocon"))
                    : ConfigurationFactory.ParseString(File.ReadAllText("config.hocon"));
                       
            var system = ActorSystem.Create("repro", config);

            var region = 
                ClusterSharding
                    .Get(system)
                    .Start("sharded", Props.Create<MyActor>(), ClusterShardingSettings.Create(system), new MyExtractor2<string>(5));

            while (Console.ReadKey().Key != ConsoleKey.Q)
            {
                for (int i = 0; i < 10; i++)
                {
                    region.Tell(i.ToString());
                    System.Threading.Thread.Sleep(100);
                }
            }
        }
    }
}
