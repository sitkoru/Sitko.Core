using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sitko.Core.Queue.Exceptions;
using Sitko.Core.Queue.Internal;

namespace Sitko.Core.Queue.InMemory
{
    public class InMemoryQueue : BaseQueue<InMemoryQueueModuleConfig>
    {
        private readonly ConcurrentDictionary<Type, InMemoryQueueChannel> _channels =
            new ConcurrentDictionary<Type, InMemoryQueueChannel>();


        public InMemoryQueue(InMemoryQueueModuleConfig config, QueueContext context,
            ILogger<InMemoryQueue> logger) : base(config, context, logger)
        {
        }

        private InMemoryQueueChannel<T> GetOrCreateChannel<T>() where T : class, new()
        {
            return _channels.GetOrAdd(typeof(T), type =>
            {
                var channel = new InMemoryQueueChannel<T>(_logger);
                channel.Run();
                return channel;
            }) as InMemoryQueueChannel<T>;
        }

        protected override Task<QueuePublishResult> DoPublishAsync<T>(QueuePayload<T> queuePayload)
        {
            var result = new QueuePublishResult();
            try
            {
                var channel = GetOrCreateChannel<T>();
                channel.Publish(queuePayload);
            }
            catch (Exception ex)
            {
                result.SetException(ex);
            }

            return Task.FromResult(result);
        }

        protected override async Task<QueuePayload<TResponse>> DoRequestAsync<TMessage, TResponse>(
            QueuePayload<TMessage> queuePayload, TimeSpan timeout)
        {
            var replyTo = Guid.NewGuid();
            var channel = GetOrCreateChannel<TResponse>();
            var resultSource = new TaskCompletionSource<QueuePayload<TResponse>>();
            var subscriptionId = channel.Subscribe(response =>
            {
                if (response.MessageContext.ReplyTo == replyTo)
                {
                    resultSource.SetResult(response);
                }

                return Task.CompletedTask;
            });
            var tasks = new List<Task> {Task.Delay(timeout), resultSource.Task};

            var sendChannel = GetOrCreateChannel<TMessage>();
            queuePayload.MessageContext.ReplyTo = replyTo;
            sendChannel.Publish(queuePayload);

            await Task.WhenAny(tasks);
            channel.UnSubscribe(subscriptionId);
            if (!resultSource.Task.IsCompleted) throw new QueueRequestTimeoutException(timeout);

            return resultSource.Task.Result;
        }

        protected override Task<QueueSubscribeResult> DoReplyAsync<TMessage, TResponse>(
            Func<QueuePayload<TMessage>, Task<QueuePayload<TResponse>?>> callback)
        {
            var result = new QueueSubscribeResult();
            try
            {
                var channel = GetOrCreateChannel<TMessage>();
                result.SubscriptionId = channel.Subscribe(async request =>
                {
                    if (request.MessageContext.ReplyTo != null)
                    {
                        var response = await callback(request);
                        if (response != null)
                        {
                            await DoPublishAsync(response);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                result.SetException(ex);
            }

            return Task.FromResult(result);
        }

        protected override Task<bool> DoStopReplyAsync<TMessage, TResponse>(Guid id)
        {
            var channel = GetOrCreateChannel<TMessage>();
            return Task.FromResult(channel.UnSubscribe(id));
        }

        protected override Task<QueueSubscribeResult> DoSubscribeAsync<T>(IQueueMessageOptions<T>? options = null)
        {
            var result = new QueueSubscribeResult();
            try
            {
                var channel = GetOrCreateChannel<T>();
                channel.Subscribe(payload =>
                    payload.MessageContext.ReplyTo == null ? ProcessMessageAsync(payload) : Task.CompletedTask);
            }
            catch (Exception ex)
            {
                result.SetException(ex);
            }

            return Task.FromResult(result);
        }

        protected override async Task DoUnsubscribeAsync<T>()
        {
            if (_channels.TryRemove(typeof(T), out var channel))
            {
                await channel.StopAsync();
            }
        }

        protected override async Task DoStopAsync()
        {
            await base.DoStopAsync();
            foreach (var channel in _channels.Values)
            {
                await channel.StopAsync();
            }

            _channels.Clear();
        }
    }
}
