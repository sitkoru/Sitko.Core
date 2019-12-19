using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.MessageBus
{
    public class MessageBus : IMessageBus
    {
        private readonly MessageBusModuleConfig _config;
        private readonly ILogger<MessageBus> _logger;
        private Channel<IMessage>? _channel;

        private readonly ConcurrentDictionary<Guid, IMessageBusSubscription> _subscriptions =
            new ConcurrentDictionary<Guid, IMessageBusSubscription>();

        private readonly List<MessageBusWorker> _workers = new List<MessageBusWorker>();

        public MessageBus(MessageBusModuleConfig config, ILogger<MessageBus> logger)
        {
            _config = config;
            _logger = logger;
        }

        public void Start(CancellationToken? cancellationToken)
        {
            if (_channel != null)
            {
                throw new InvalidOperationException("Message bus already started");
            }
            _channel = _config.QueueLength > 0
                ? Channel.CreateBounded<IMessage>(_config.QueueLength)
                : Channel.CreateUnbounded<IMessage>();
            for (var i = 0; i < _config.WorkersCount; i++)
            {
                var worker = new MessageBusWorker(_channel.Reader,
                    message => ProcessMessageAsync(message, cancellationToken), _logger);
                worker.Run();
                _workers.Add(worker);
            }
        }

        private async Task ProcessMessageAsync(IMessage message, CancellationToken? cancellationToken)
        {
            foreach (var subscription in _subscriptions.Values.Where(s => s.CanProcess(message)))
            {
                await subscription.ProcessAsync(message, cancellationToken);
            }
        }

        public async Task StopAsync(CancellationToken? cancellationToken)
        {
            if (_channel == null)
            {
                throw new InvalidOperationException("Message bus wasn't started yet");
            }

            foreach (var worker in _workers)
            {
                worker.Stop();
            }

            _channel.Writer.TryComplete();
            await _channel.Reader.Completion;
            _channel = null;
            _workers.Clear();
        }

        public void Publish<T>(T message) where T : IMessage
        {
            if (_channel == null)
            {
                throw new InvalidOperationException("Message bus wasn't started yet");
            }

            var result = _channel.Writer.TryWrite(message);
            if (!result)
            {
                throw new Exception("Channel length exceed limit");
            }
        }

        public Guid SubscribeAll(Func<IMessage, CancellationToken?, Task<bool>> callback)
        {
            var subscription = new MessageBusSubscription(callback);
            _subscriptions[subscription.Id] = subscription;
            return subscription.Id;
        }

        public Guid Subscribe<T>(Func<T, CancellationToken?, Task<bool>> callback) where T : IMessage
        {
            var subscription = new MessageBusSubscription<T>(callback);
            _subscriptions[subscription.Id] = subscription;
            return subscription.Id;
        }

        public void Unsubscribe(Guid subscriptionId)
        {
            if (!_subscriptions.TryRemove(subscriptionId, out _))
            {
                throw new Exception($"Can't remove subscription {subscriptionId}");
            }
        }
    }
}
