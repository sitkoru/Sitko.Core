using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Sitko.Core.PersistentQueue.HostedService;

namespace Sitko.Core.PersistentQueue.InMemory
{
    public class InMemoryQueueConnection : IPersistentQueueConnection
    {
        private ConcurrentDictionary<string, Channel<InMemoryPersistentQueueMessage>> _channels =
            new ConcurrentDictionary<string, Channel<InMemoryPersistentQueueMessage>>();

        private Channel<InMemoryPersistentQueueMessage> GetChannel(string queue)
        {
            return _channels.GetOrAdd(queue, Channel.CreateUnbounded<InMemoryPersistentQueueMessage>());
        }

        public Guid Id { get; } = Guid.NewGuid();

        public async Task PublishAsync(string queue, byte[] payload)
        {
            var channel = GetChannel(queue);
            await channel.Writer.WriteAsync(new InMemoryPersistentQueueMessage(payload));
        }

        public void Publish(string queue, byte[] payload)
        {
            var channel = GetChannel(queue);
            channel.Writer.TryWrite(new InMemoryPersistentQueueMessage(payload));
        }

        public async Task<byte[]> RequestAsync(string queue, byte[] payload, int timeOut)
        {
            var replyTo = Guid.NewGuid().ToString();
            var replyChannel = GetChannel(replyTo);
            var channel = GetChannel(queue);
            await channel.Writer.WriteAsync(new InMemoryPersistentQueueMessage(payload, replyTo));
            var tasks = new List<Task> {Task.Delay(timeOut), replyChannel.Reader.WaitToReadAsync().AsTask()};
            await Task.WhenAny(tasks);

            if (replyChannel.Reader.TryRead(out var data))
            {
                await UnSubscribeAsync(replyTo);
                return data.Data;
            }

            replyChannel.Writer.Complete();
            await UnSubscribeAsync(replyTo);
            throw new Exception("Wait timeout");
        }

        public Task SubscribeAsync(PersistedQueueHostedServiceOptions options, string queue,
            Func<PersistentQueueMessage, Task> callback)
        {
            var channel = GetChannel(queue);
            Task.Run(async () =>
            {
                while (await channel.Reader.WaitToReadAsync())
                {
                    var msg = await channel.Reader.ReadAsync();
                    await callback(msg);
                }
            });
            return Task.CompletedTask;
        }

        public Task SubscribeWithResponseAsync(PersistedQueueHostedServiceOptions options, string queue,
            Func<PersistentQueueMessage, Task> callback)
        {
            return SubscribeAsync(options, queue, callback);
        }

        public Task UnSubscribeAsync(string queue)
        {
            if (_channels.TryRemove(queue, out var channel))
            {
                channel.Writer.Complete();
            }

            return Task.CompletedTask;
        }

        public ConnectionHealthStatus GetHealthStatus()
        {
            return ConnectionHealthStatus.Healthy;
        }

        public string GetHealthMessage()
        {
            return "Ok";
        }
    }
}
