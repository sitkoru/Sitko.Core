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
        private readonly Channel<(T message, QueueMessageContext messageContext)> _channel = Channel.CreateUnbounded<(T message, QueueMessageContext messageContext)>();
        private readonly ILogger _logger;

        private readonly ConcurrentDictionary<Guid, Func<T, QueueMessageContext, Task>> _callbacks =
            new ConcurrentDictionary<Guid, Func<T, QueueMessageContext, Task>>();

        private readonly Guid _id = Guid.NewGuid();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public InMemoryQueueChannel(ILogger logger)
        {
            _logger = logger;
        }

        public void Publish(T message, QueueMessageContext context)
        {
            _channel.Writer.TryWrite((message, context));
        }

        public Guid Subscribe(Func<T, QueueMessageContext, Task> callback)
        {
            var id = Guid.NewGuid();
            _callbacks.TryAdd(id, callback);
            return id;
        }

        public bool UnSubscribe(Guid id)
        {
            return _callbacks.TryRemove(id, out _);
        }

        public void Run()
        {
            _logger.LogInformation("Start message bus worker {Id}", _id);
            Task.Run(async () =>
            {
                _logger.LogInformation("Message bus worker {Id} processing messages", _id);
                await foreach (var entry in _channel.Reader.ReadAllAsync(_cts.Token))
                {
                    _logger.LogDebug("Message bus worker {Id} got new message {@Message}", _id, entry);
                    foreach (var callback in _callbacks.Values)
                    {
                        try
                        {
                            await callback(entry.message, entry.messageContext);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error while processing message {@Message}: {ErrorText}", entry,
                                ex.ToString());
                        }
                    }

                    _logger.LogDebug("Message {@Message} processed by message bus worker {Id}", entry, _id);
                }

                _logger.LogInformation("Message bus worker {Id} stop processing messages", _id);
            }, _cts.Token);
            _logger.LogInformation("Message bus worker {Id} started", _id);
        }

        public override async Task StopAsync()
        {
            _channel.Writer.TryComplete();
            _logger.LogInformation("Stop message bus worker {Id}", _id);
            _cts.Cancel();
            _logger.LogInformation("Message bus worker {Id} stopped", _id);
            await _channel.Reader.Completion;
        }
    }

    public abstract class InMemoryQueueChannel
    {
        public abstract Task StopAsync();
    }
}
