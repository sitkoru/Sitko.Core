using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NATS.Client;
using Sitko.Core.PersistentQueue.HostedService;
using STAN.Client;

namespace Sitko.Core.PersistentQueue.Nats
{
    public class NatsQueueConnection : IPersistentQueueConnection, IDisposable
    {
        private IConnection _natsConn;
        private readonly PersistentQueueModuleOptions _options;
        internal IStanConnection StanConnection { get; }

        private readonly ConcurrentDictionary<string, IStanSubscription> _stanSubscriptions =
            new ConcurrentDictionary<string, IStanSubscription>();

        private readonly ConcurrentDictionary<string, IAsyncSubscription> _natsSubscriptions =
            new ConcurrentDictionary<string, IAsyncSubscription>();

        public NatsQueueConnection(IStanConnection stanConnection, IConnection natsConnection,
            PersistentQueueModuleOptions options)
        {
            StanConnection = stanConnection;
            _natsConn = natsConnection;
            _options = options;
        }

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            foreach (var stanSubscription in _stanSubscriptions.Values)
            {
                stanSubscription.Unsubscribe();
            }

            foreach (var natsSubscription in _natsSubscriptions.Values)
            {
                natsSubscription.Unsubscribe();
            }

            StanConnection.Close();
            StanConnection.Dispose();
            _natsConn.Close();
            _natsConn.Dispose();

            _disposed = true;
        }

        public Guid Id { get; } = Guid.NewGuid();

        public Task PublishAsync(string queue, byte[] payload)
        {
            return StanConnection.PublishAsync(queue, payload);
        }

        public void Publish(string queue, byte[] payload)
        {
            StanConnection.Publish(queue, payload);
        }

        public async Task<byte[]> RequestAsync(string queue, byte[] payload, int timeOut)
        {
            var result =
                await StanConnection.NATSConnection.RequestAsync(queue, payload,
                    timeOut);
            return result.Data;
        }

        public Task SubscribeAsync(PersistedQueueHostedServiceOptions queueOptions, string queue,
            Func<PersistentQueueMessage, Task> callback)
        {
            var options = StanSubscriptionOptions.GetDefaultOptions();
            if (queueOptions.All)
            {
                options.DeliverAllAvailable();
                queueOptions.Durable = false;
            }
            else
            {
                if (queueOptions.StartAt.HasValue)
                {
                    options.StartAt(queueOptions.StartAt.Value);
                    queueOptions.Durable = false;
                }
            }

            if (queueOptions.ManualAck)
            {
                options.AckWait = queueOptions.AckWait;
                options.ManualAcks = true;
            }

            if (queueOptions.MaxInFlight > 0)
            {
                options.MaxInflight = queueOptions.MaxInFlight;
            }

            IStanSubscription sub;
            if (queueOptions.Durable && !string.IsNullOrEmpty(_options.ConsumerGroupName))
            {
                options.DurableName = _options.ConsumerGroupName;
                sub = StanConnection.Subscribe(queue, _options.ConsumerGroupName, options,
                    async (sender, args) =>
                        await callback(new StanPersistentQueueMessage(args)));
            }
            else
            {
                sub = StanConnection.Subscribe(queue, options,
                    async (sender, args) => await callback(new StanPersistentQueueMessage(args)));
            }

            _stanSubscriptions.TryAdd(queue, sub);

            return Task.CompletedTask;
        }

        public Task SubscribeWithResponseAsync(PersistedQueueHostedServiceOptions options, string queue,
            Func<PersistentQueueMessage, Task> callback)
        {
            var sub = StanConnection.NATSConnection.SubscribeAsync(queue,
                async (sender, args) => await callback(new NatsPersistentQueueMessage(args)));
            _natsSubscriptions.TryAdd(queue, sub);
            return Task.CompletedTask;
        }

        public Task UnSubscribeAsync(string queue)
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }

            if (_stanSubscriptions.TryRemove(queue, out var stanSubscription))
            {
                stanSubscription.Unsubscribe();
            }

            if (_natsSubscriptions.TryRemove(queue, out var natsSubscription))
            {
                natsSubscription.Unsubscribe();
            }

            return Task.CompletedTask;
        }

        public bool IsHealthy()
        {
            return _natsConn.State == ConnState.CONNECTED;
        }

        public string? GetLastError()
        {
            return _natsConn.LastError.ToString();
        }

        public class NatsPersistentQueueMessage : PersistentQueueMessage
        {
            public NatsPersistentQueueMessage(MsgHandlerEventArgs message) : base(message.Message.Data,
                message.Message.Reply)
            {
            }

            public override Task Ack()
            {
                return Task.CompletedTask;
            }
        }

        public class StanPersistentQueueMessage : PersistentQueueMessage
        {
            private StanMsg _message;

            public StanPersistentQueueMessage(StanMsgHandlerArgs message) : base(message.Message.Data, "")
            {
                _message = message.Message;
            }

            public override Task Ack()
            {
                _message.Ack();
                return Task.CompletedTask;
            }
        }
    }
}
