using System;

namespace Sitko.Core.PersistentQueue.InMemory
{
    public class InMemoryPersistentQueueModule<T> : PersistentQueueModule<T, InMemoryPersistentQueueModuleOptions,
        InMemoryQueueConnection, InMemoryConnectionFactory>
    {
        protected override Type GetConsumerType()
        {
            return typeof(InMemoryQueueConsumer<>);
        }

        protected override Type GetProducerType()
        {
            return typeof(InMemoryQueueProducer<>);
        }
    }

    public class InMemoryPersistentQueueModuleOptions : PersistentQueueModuleOptions
    {
    }
}
