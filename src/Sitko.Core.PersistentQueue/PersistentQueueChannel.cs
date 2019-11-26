using System;
using Sitko.Core.PersistentQueue.Internal;

namespace Sitko.Core.PersistentQueue
{
    public abstract class PersistentQueueChannel<TConnection> : IDisposable
        where TConnection : IPersistentQueueConnection
    {
        protected readonly IPersistentQueueConnectionFactory<TConnection> _connectionFactory;
        protected readonly PersistentQueueMessageSerializer _serializer = new PersistentQueueMessageSerializer();

        protected PersistentQueueChannel(IPersistentQueueConnectionFactory<TConnection> connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public virtual void Dispose()
        {
            _connectionFactory?.Dispose();
        }
    }
}
