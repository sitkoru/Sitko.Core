using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private bool _isStarted;

        private readonly ConcurrentDictionary<Guid, QueueSubscription> _subscriptions =
            new ConcurrentDictionary<Guid, QueueSubscription>();

        protected BaseQueue(TConfig config, QueueContext context, ILogger<BaseQueue<TConfig>> logger)
        {
            _config = config;
            _logger = logger;
            _middlewares = context.Middlewares;
            _messageOptions = context.MessageOptions;
        }

        private async Task<bool> OnBeforeReceiveMessageAsync<T>(QueuePayload<T> payload) where T : class
        {
            foreach (var queueMiddleware in _middlewares)
            {
                var result = await queueMiddleware.OnBeforeReceiveAsync(payload.Message, payload.MessageContext);
                if (!result)
                {
                    return false;
                }
            }

            return true;
        }

        private async Task<QueuePublishResult> OnBeforePublishMessageAsync<T>(QueuePayload<T> payload)
            where T : class
        {
            foreach (var queueMiddleware in _middlewares)
            {
                var result = await queueMiddleware.OnBeforePublishAsync(payload.Message, payload.MessageContext);
                if (!result.IsSuccess)
                {
                    return result;
                }
            }

            return new QueuePublishResult();
        }

        private async Task OnAfterReceiveMessageAsync<T>(QueuePayload<T> payload) where T : class
        {
            foreach (var queueMiddleware in _middlewares)
            {
                await queueMiddleware.OnAfterReceiveAsync(payload.Message, payload.MessageContext);
            }
        }

        private async Task OnAfterPublishMessageAsync<T>(QueuePayload<T> payload) where T : class
        {
            foreach (var queueMiddleware in _middlewares)
            {
                await queueMiddleware.OnAfterPublishAsync(payload.Message, payload.MessageContext);
            }
        }

        protected async Task<bool> ProcessMessageAsync<T>(QueuePayload<T> payload) where T : class
        {
            var subscriptions = _subscriptions.Values.Where(s => s is QueueSubscription<T>).ToList();

            if (!subscriptions.Any())
            {
                return true;
            }

            if (!await OnBeforeReceiveMessageAsync(payload))
            {
                return false;
            }

            foreach (var subscription in subscriptions)
            {
                if (!(subscription is QueueSubscription<T> messageSubscription)) continue;

                try
                {
                    await messageSubscription.ProcessAsync(payload);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while processing message {MessageType}: {ErrorText}", typeof(T),
                        ex.ToString());
                }
            }

            await OnAfterReceiveMessageAsync(payload);

            return true;
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

            return messageContext;
        }

        protected abstract Task<QueuePublishResult> DoPublishAsync<T>(QueuePayload<T> queuePayload)
            where T : class;

        protected abstract Task<QueuePayload<TResponse>> DoRequestAsync<TMessage, TResponse>(
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
            if (!_isStarted)
            {
                await DoStartAsync();
                _isStarted = true;
            }
        }

        public async Task<QueuePublishResult> PublishAsync<T>(T message,
            QueueMessageContext? parentMessageContext = null)
            where T : class
        {
            await StartAsync();
            var messageContext = GetMessageContext<T>(parentMessageContext);

            var payload = new QueuePayload<T>(message, messageContext);
            var result = await OnBeforePublishMessageAsync(payload);
            if (!result.IsSuccess)
            {
                return result;
            }

            result = await DoPublishAsync(payload);
            await OnAfterPublishMessageAsync(payload);
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
                    await callback(payload.Message, payload.MessageContext));
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
                if (await OnBeforeReceiveMessageAsync(request))
                {
                    var response = await callback(request.Message, request.MessageContext);
                    var responsePayload = new QueuePayload<TResponse>(response, request.MessageContext);
                    if ((await OnBeforePublishMessageAsync(responsePayload)).IsSuccess)
                    {
                        return responsePayload;
                    }

                    await OnAfterPublishMessageAsync(responsePayload);
                    await OnAfterReceiveMessageAsync(request);
                }

                return null;
            });
        }

        public Task<bool> StopReplyAsync<TMessage, TResponse>(Guid id)where TMessage : class
            where TResponse : class
        {
            return DoStopReplyAsync<TMessage, TResponse>(id);
        }


        public async Task<(TResponse message, QueueMessageContext messageContext)> RequestAsync<TMessage, TResponse>(
            TMessage message, QueueMessageContext? parentMessageContext = null, TimeSpan? timeout = null)
            where TMessage : class
            where TResponse : class
        {
            await StartAsync();
            timeout ??= TimeSpan.FromSeconds(5);

            var messageContext = GetMessageContext<TMessage>(parentMessageContext);
            var payload = new QueuePayload<TMessage>(message, messageContext);
            var beforeResult = await OnBeforePublishMessageAsync(payload);
            if (!beforeResult.IsSuccess)
            {
                if (beforeResult.Exception != null)
                {
                    throw beforeResult.Exception;
                }

                throw new Exception(beforeResult.ErrorMessage);
            }

            var response = await DoRequestAsync<TMessage, TResponse>(payload, timeout.Value);
            await OnAfterPublishMessageAsync(payload);
            if (await OnBeforeReceiveMessageAsync(response))
            {
                await OnAfterReceiveMessageAsync(response);
                return (response.Message, response.MessageContext);
            }

            throw new Exception("Processing error");
        }

        public async ValueTask DisposeAsync()
        {
            _subscriptions.Clear();
            await DoStopAsync();
        }
    }
}
