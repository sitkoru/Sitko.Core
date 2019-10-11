using System;
using System.Threading.Tasks;
using NATS.Client;
using STAN.Client;

namespace Sitko.Core.PersistentQueue.Internal
{
    internal class PersistentQueueEmulatedConnection : IStanConnection
    {
        public void Dispose()
        {
        }

        public void Publish(string subject, byte[] data)
        {
        }

        public string Publish(string subject, byte[] data, EventHandler<StanAckHandlerArgs> handler)
        {
            return string.Empty;
        }

        public Task<string> PublishAsync(string subject, byte[] data)
        {
            return Task.FromResult(string.Empty);
        }

        public IStanSubscription Subscribe(string subject, EventHandler<StanMsgHandlerArgs> handler)
        {
            return new PersistentQueueEmulatedSubscription();
        }

        public IStanSubscription Subscribe(string subject, StanSubscriptionOptions options,
            EventHandler<StanMsgHandlerArgs> handler)
        {
            return new PersistentQueueEmulatedSubscription();
        }

        public IStanSubscription Subscribe(string subject, string qgroup, EventHandler<StanMsgHandlerArgs> handler)
        {
            return new PersistentQueueEmulatedSubscription();
        }

        public IStanSubscription Subscribe(string subject, string qgroup, StanSubscriptionOptions options,
            EventHandler<StanMsgHandlerArgs> handler)
        {
            return new PersistentQueueEmulatedSubscription();
        }

        public void Close()
        {
        }

        public IConnection NATSConnection { get; } = new NatsEmulatedConnection();
    }
}
