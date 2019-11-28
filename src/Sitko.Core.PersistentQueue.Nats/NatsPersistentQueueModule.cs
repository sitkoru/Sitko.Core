using System;
using System.Collections.Generic;

namespace Sitko.Core.PersistentQueue.Nats
{
    public class NatsPersistentQueueModule<TAssembly>
        : PersistentQueueModule<TAssembly, NatsPersistentQueueModuleOptions, NatsQueueConnection, NatsConnectionFactory>
    {
        protected override Type GetConsumerType()
        {
            return typeof(NatsQueueConsumer<>);
        }

        protected override Type GetProducerType()
        {
            return typeof(NatsQueueProducer<>);
        }
    }

    public class NatsPersistentQueueModuleOptions : PersistentQueueModuleOptions
    {
        public readonly List<(string host, int port)> Servers = new List<(string host, int port)>();
        public string ClusterName { get; set; }
        public string ClientName { get; set; }
    }
}
