using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Queue.InMemory
{
    public class InMemoryQueueChannel<T> : InMemoryQueueChannel where T : class
    {
        private readonly Channel<(T message, QueueMessageContext messageContext)> channel = Channel.CreateUnbounded<(T message, QueueMessageContext messageContext)>();
        private readonly ILogger logger;

        private readonly ConcurrentDictionary<Guid, Func<T, QueueMessageContext, Task>> callbacks = new();

        private readonly Guid id = Guid.NewGuid();
        private readonly CancellationTokenSource cts = new();

        public InMemoryQueueChannel(ILogger logger) => this.logger = logger;

        public void Publish(T message, QueueMessageContext context) => channel.Writer.TryWrite((message, context));

        public Guid Subscribe(Func<T, QueueMessageContext, Task> callback)
        {
            var subscriptionId = Guid.NewGuid();
            callbacks.TryAdd(subscriptionId, callback);
            return subscriptionId;
        }

        public bool UnSubscribe(Guid subscriptionId) => callbacks.TryRemove(subscriptionId, out _);

        public void Run()
        {
            logger.LogInformation("Start message bus worker {Id}", id);
            Task.Run(async () =>
            {
                logger.LogInformation("Message bus worker {Id} processing messages", id);
                await foreach (var entry in channel.Reader.ReadAllAsync(cts.Token))
                {
                    logger.LogDebug("Message bus worker {Id} got new message {@Message}", id, entry);
                    foreach (var callback in callbacks.Values)
                    {
                        try
                        {
                            await callback(entry.message, entry.messageContext);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error while processing message {@Message}: {ErrorText}", entry,
                                ex.ToString());
                        }
                    }

                    logger.LogDebug("Message {@Message} processed by message bus worker {Id}", entry, id);
                }

                logger.LogInformation("Message bus worker {Id} stop processing messages", id);
            }, cts.Token);
            logger.LogInformation("Message bus worker {Id} started", id);
        }

        public override async Task StopAsync()
        {
            channel.Writer.TryComplete();
            logger.LogInformation("Stop message bus worker {Id}", id);
            cts.Cancel();
            logger.LogInformation("Message bus worker {Id} stopped", id);
            await channel.Reader.Completion;
        }
    }

    public abstract class InMemoryQueueChannel
    {
        public abstract Task StopAsync();
    }
}
