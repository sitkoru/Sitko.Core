using System;
using System.Threading.Tasks;
using Sitko.Core.PersistentQueue.HostedService;

namespace Sitko.Core.PersistentQueue
{
    public interface IPersistentQueueConnection
    {
        Guid Id { get; }
        Task PublishAsync(string queue, byte[] payload);
        void Publish(string queue, byte[] payload);
        Task<byte[]> RequestAsync(string queue, byte[] payload, int timeOut);

        Task SubscribeAsync(PersistedQueueHostedServiceOptions options, string queue,
            Func<PersistentQueueMessage, Task> callback);

        Task SubscribeWithResponseAsync(PersistedQueueHostedServiceOptions options, string queue,
            Func<PersistentQueueMessage, Task> callback);

        Task UnSubscribeAsync(string queue);

        bool IsHealthy();
        string? GetLastError();
    }

    public abstract class PersistentQueueMessage
    {
        protected PersistentQueueMessage(byte[] data, string replyTo)
        {
            Data = data;
            ReplyTo = replyTo;
        }

        public byte[] Data { get; }
        public string ReplyTo { get; }
        public abstract Task Ack();
    }
}
