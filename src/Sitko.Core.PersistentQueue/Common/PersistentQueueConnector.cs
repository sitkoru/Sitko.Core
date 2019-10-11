using System;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Sitko.Core.PersistentQueue.Internal;
using STAN.Client;

namespace Sitko.Core.PersistentQueue.Common
{
    public class PersistentQueueConnector<T> : PersistentQueueConnector where T : IMessage
    {
        public PersistentQueueConnector(IPersistentQueueConnectionFactory factory, PersistentQueueConnection connection, ILogger logger) : base(
            factory,
            connection, logger)
        {
            MessageType = typeof(T);
        }
    }

    public class PersistentQueueConnector : IDisposable
    {
        private PersistentQueueConnection _connection;
        private readonly IPersistentQueueConnectionFactory _factory;
        private readonly ILogger _logger;
        public Guid Id { get; }
        public Type MessageType { get; protected set; }

        internal IStanConnection Connection => _connection.Connection;
        internal event ReconnectedEventHandler Reconnected;

        public PersistentQueueConnector(IPersistentQueueConnectionFactory factory, PersistentQueueConnection connection, ILogger logger)
        {
            _factory = factory;
            _logger = logger;
            _connection = null;
            Reconnected = null;
            Id = Guid.NewGuid();
            SetConnection(connection);
        }

        internal void SetConnection(PersistentQueueConnection connection)
        {
            _connection = connection;
            var connector = this;
            _connection.Reconnected += (sender, args) =>
            {
                _logger.LogWarning("Connector {Id} ({MessageType}) connection is reconnected", Id, MessageType);
                connector.Reconnected?.Invoke(connector, args);
            };
        }

        public void Reconnect(PersistentQueueConnection connection)
        {
            SetConnection(connection);
            _logger.LogWarning("Connector {Id} ({MessageType}) have new connection", Id, MessageType);
            Reconnected?.Invoke(this, new ReconnectedEventHandlerArgs());
        }

        public void Dispose()
        {
            _factory.ReleaseConnector(this, _connection);
        }
    }
}
