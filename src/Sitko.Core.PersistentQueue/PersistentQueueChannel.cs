using System;
using Sitko.Core.PersistentQueue.Internal;

namespace Sitko.Core.PersistentQueue
{
    public abstract class PersistentQueueChannel<TConnection>
        where TConnection : IPersistentQueueConnection
    {
        protected readonly IPersistentQueueConnectionFactory<TConnection> _connectionFactory;
        protected readonly PersistentQueueMessageSerializer _serializer = new PersistentQueueMessageSerializer();

        protected PersistentQueueChannel(IPersistentQueueConnectionFactory<TConnection> connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
    }
}
