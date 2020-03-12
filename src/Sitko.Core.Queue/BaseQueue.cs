using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Sitko.Core.Queue.Internal;

namespace Sitko.Core.Queue
{
    public abstract class BaseQueue<TConfig> : IQueue where TConfig : QueueModuleConfig
    {
        protected readonly TConfig _config;
        private readonly List<IQueueMiddleware> _middlewares;
        private readonly List<IQueueMessageOptions> _messageOptions;

        protected readonly ILogger<BaseQueue<TConfig>> _logger;
        protected bool IsStarted { get; private set; }

        private readonly ConcurrentDictionary<Guid, QueueSubscription> _subscriptions =
            new ConcurrentDictionary<Guid, QueueSubscription>();

        protected BaseQueue(TConfig config, QueueContext context, ILogger<BaseQueue<TConfig>> logger)
        {
            _config = config;
            _logger = logger;
            _middlewares = context.Middleware;
            _messageOptions = context.MessageOptions;
        }

        private async Task<bool> ReceiveAsync<T>(QueuePayload<T> payload) where T : class
        {
            var subscriptions = _subscriptions.Values.Where(s => s is QueueSubscription<T>).ToList();

            if (!subscriptions.Any())
            {
                return false;
            }

            foreach (var subscription in subscriptions)
            {
                if (subscription is QueueSubscription<T> payloadSubscription)
                {
                    try
                    {
                        await payloadSubscription.ProcessAsync(payload);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while processing message {MessageType}: {ErrorText}",
                            payload.Message.GetType(),
                            ex.ToString());
                    }
                }
            }

            return true;
        }

        protected Task<bool> ProcessMessageAsync<T>(QueuePayload<T> payload,
            Func<QueuePayload<T>, Task<bool>>? callback = null) where T : class
        {
            callback ??= ReceiveAsync;
            Func<QueuePayload<T>, Task<bool>> pipeLine = callback;
            foreach (var middleware in _middlewares)
            {
                callback = pipeLine;
                pipeLine = queuePayload => middleware.ReceiveAsync(queuePayload, callback);
            }

            return pipeLine(payload);
        }

        private QueueMessageContext GetMessageContext<T>(QueueMessageContext? parentMessageContext = null)
        {
            var messageContext = new QueueMessageContext
            {
                MessageType = typeof(T).FullName,
                RequestId = parentMessageContext?.RequestId,
                ParentMessageId = parentMessageContext?.Id,
                RootMessageId = parentMessageContext?.RootMessageId,
                RootMessageDate = parentMessageContext?.RootMessageDate,
                ReplyTo = parentMessageContext?.ReplyTo
            };

            if (parentMessageContext?.RootMessageId != null)
            {
                messageContext.RootMessageId = parentMessageContext.RootMessageId;
                messageContext.RootMessageDate = parentMessageContext.RootMessageDate;
            }
            else
            {
                messageContext.RootMessageId = messageContext.Id;
                messageContext.RootMessageDate = messageContext.Date;
            }


            return messageContext;
        }

        protected abstract Task<QueuePublishResult> DoPublishAsync<T>(QueuePayload<T> queuePayload)
            where T : class;

        protected abstract Task<QueuePayload<TResponse>?> DoRequestAsync<TMessage, TResponse>(
            QueuePayload<TMessage> queuePayload, TimeSpan timeout)
            where TMessage : class
            where TResponse : class;

        protected abstract Task<QueueSubscribeResult> DoReplyAsync<TMessage, TResponse>(
            Func<QueuePayload<TMessage>, Task<QueuePayload<TResponse>?>> callback)
            where TMessage : class
            where TResponse : class;

        protected abstract Task<bool> DoStopReplyAsync<TMessage, TResponse>(Guid id)
            where TMessage : class
            where TResponse : class;

        protected abstract Task<QueueSubscribeResult> DoSubscribeAsync<T>(IQueueMessageOptions<T>? options = null)
            where T : class;

        protected abstract Task DoUnsubscribeAsync<T>() where T : class;

        private IQueueMessageOptions<T>? GetOptions<T>() where T : class
        {
            return _messageOptions.FirstOrDefault(o => o is IQueueMessageOptions<T>) as IQueueMessageOptions<T>;
        }

        protected virtual Task DoStartAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual Task DoStopAsync()
        {
            return Task.CompletedTask;
        }

        protected async Task StartAsync()
        {
            if (!IsStarted)
            {
                await DoStartAsync();
                IsStarted = true;
            }
        }

        private Task<QueuePublishResult> PublishAsync<T>(QueuePayload<T> payload,
            Func<QueuePayload<T>, Task<QueuePublishResult>>? callback = null) where T : class
        {
            callback ??= DoPublishAsync;
            Func<QueuePayload<T>, Task<QueuePublishResult>> pipeLine = callback;
            foreach (var middleware in _middlewares)
            {
                callback = pipeLine;
                pipeLine = queuePayload => middleware.PublishAsync(queuePayload, callback);
            }

            return pipeLine(payload);
        }

        public async Task<QueuePublishResult> PublishAsync<T>(T message,
            QueueMessageContext? parentMessageContext = null)
            where T : class
        {
            await StartAsync();
            var messageContext = GetMessageContext<T>(parentMessageContext);

            var payload = new QueuePayload<T>(message, messageContext);

            var result = await PublishAsync(payload);

            return result;
        }


        public async Task<QueueSubscribeResult> SubscribeAsync<T>(
            Func<T, QueueMessageContext, Task<bool>> callback)
            where T : class
        {
            await StartAsync();
            var options = GetOptions<T>();
            var result = await DoSubscribeAsync(options);
            if (result.IsSuccess)
            {
                var subscription = new QueueSubscription<T>(async payload =>
                    await callback(payload.Message, payload.MessageContext!));
                _subscriptions[subscription.Id] = subscription;
                result.SubscriptionId = subscription.Id;
                result.Options = options;
            }

            return result;
        }

        public async Task UnsubscribeAsync<T>(Guid subscriptionId) where T : class
        {
            if (!(_subscriptions[subscriptionId] is QueueSubscription<T>))
            {
                throw new Exception($"Subscription {subscriptionId} is not for type {typeof(T)}");
            }

            var unsubscribed = _subscriptions.TryRemove(subscriptionId, out QueueSubscription _);
            if (!unsubscribed)
            {
                throw new Exception($"Can't remove subscription {subscriptionId}");
            }

            if (!_subscriptions.Values.Any(s => s is QueueSubscription<T>))
            {
                await DoUnsubscribeAsync<T>();
            }
        }

        public async Task<QueueSubscribeResult> ReplyAsync<TMessage, TResponse>(
            Func<TMessage, QueueMessageContext, Task<TResponse>> callback)
            where TMessage : class where TResponse : class
        {
            await StartAsync();
            return await DoReplyAsync<TMessage, TResponse>(async request =>
            {
                QueuePayload<TResponse>? responsePayload = null;
                if (await ProcessMessageAsync(request, async payload =>
                {
                    var response = await callback(request.Message, request.MessageContext!);
                    var rPayload = new QueuePayload<TResponse>(response, request.MessageContext);
                    var result = await PublishAsync(rPayload,
                        queuePayload => Task.FromResult(new QueuePublishResult()));
                    if (!result.IsSuccess) return false;
                    responsePayload = rPayload;
                    return true;
                }))
                {
                    return responsePayload;
                }

                return null;
            });
        }

        public Task<bool> StopReplyAsync<TMessage, TResponse>(Guid id) where TMessage : class
            where TResponse : class
        {
            return DoStopReplyAsync<TMessage, TResponse>(id);
        }

        public abstract Task<(HealthStatus status, string? errorMessage)> CheckHealthAsync();


        public async Task<(TResponse message, QueueMessageContext messageContext)?> RequestAsync<TMessage, TResponse>(
            TMessage message, QueueMessageContext? parentMessageContext = null, TimeSpan? timeout = null)
            where TMessage : class
            where TResponse : class
        {
            await StartAsync();
            timeout ??= TimeSpan.FromSeconds(5);

            var messageContext = GetMessageContext<TMessage>(parentMessageContext);
            var payload = new QueuePayload<TMessage>(message, messageContext);
            QueuePayload<TResponse>? response = null;
            (TResponse message, QueueMessageContext messageContext)? result = null;
            var publishResult = await PublishAsync(payload, async queuePayload =>
            {
                response = await DoRequestAsync<TMessage, TResponse>(payload, timeout.Value);
                return new QueuePublishResult();
            });
            if (publishResult.IsSuccess && response != null)
            {
                if (await ProcessMessageAsync(response, queuePayload => Task.FromResult(true)))
                {
                    result = (response.Message, response.MessageContext!);
                }
                else
                {
                    throw new Exception("Processing error");
                }
            }

            return result;
        }

        public async ValueTask DisposeAsync()
        {
            _subscriptions.Clear();
            await DoStopAsync();
        }
    }
}
