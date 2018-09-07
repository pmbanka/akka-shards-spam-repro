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
        /// Generates shardId/entityId pairs like
        /// 1/1
        /// 2/2
        /// 3/3 etc. 
        /// </summary>
        class MyExtractor<T> : IMessageExtractor
        {
            public readonly int MaxNumberOfShards;
            private readonly ILoggingAdapter _log;

            public MyExtractor(int maxNumberOfShards, ILoggingAdapter log)
            {
                MaxNumberOfShards = maxNumberOfShards;
                _log = log;
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
                _log.Info("Extracted entityId [{0}] from message [{1}]", id, message);
                return id;
            }

            public object EntityMessage(object message)
            {
                return message;
            }

            public string ShardId(object message)
            {
                var id = EntityId(message);
                _log.Info("Extracted shardId [{0}] from message [{1}]", id, message);
                return id;
            }
        }
        
        class MyActor: ReceiveActor
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
                    .Start("sharded", Props.Create<MyActor>(), ClusterShardingSettings.Create(system), new MyExtractor<string>(5, system.Log));

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
