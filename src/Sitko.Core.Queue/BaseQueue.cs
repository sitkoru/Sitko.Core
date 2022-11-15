using System.Collections.Concurrent;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.Queue.Internal;

namespace Sitko.Core.Queue;

public abstract class BaseQueue<TConfig> : IQueue where TConfig : QueueModuleOptions
{
    private readonly IOptionsMonitor<TConfig> config;

    //private readonly IList<IQueueMiddleware> _middlewares;
    private readonly IList<IQueueMessageOptions> messageOptions;

    private readonly QueuePipeline pipeline;

    private readonly ConcurrentDictionary<Guid, QueueSubscription> subscriptions = new();

    protected BaseQueue(IOptionsMonitor<TConfig> config, QueueContext context, ILogger<BaseQueue<TConfig>> logger)
    {
        this.config = config;
        Logger = logger;
        //_middlewares = context.Middleware;
        pipeline = new QueuePipeline();
        foreach (var middleware in context.Middleware)
        {
            pipeline.Use(middleware);
        }

        messageOptions = context.MessageOptions;
    }

    protected TConfig Config => config.CurrentValue;

    protected ILogger<BaseQueue<TConfig>> Logger { get; }
    protected bool IsStarted { get; private set; }

    // private Task<QueuePublishResult> PublishAsync<T>(QueuePayload<T> payload,
    //     Func<T, QueueMessageContext, Task<QueuePublishResult>>? callback = null) where T : class
    // {
    //     callback ??= (message, context) => DoPublishAsync(new QueuePayload<T>(message, context));
    //     return _pipeline.PublishAsync(payload.Message, payload.MessageContext, callback);
    // }

    public async Task<QueuePublishResult> PublishAsync<T>(T message,
        QueueMessageContext? parentMessageContext = null)
        where T : class
    {
        await StartAsync();
        var messageContext = GetMessageContext<T>(parentMessageContext);

        var result = await pipeline.PublishAsync(message, messageContext, DoPublishAsync);

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
            var subscription = new QueueSubscription<T>(async (message, context) =>
                await callback(message, context));
            subscriptions[subscription.Id] = subscription;
            result.SubscriptionId = subscription.Id;
            result.Options = options;
        }

        return result;
    }

    public async Task UnsubscribeAsync<T>(Guid subscriptionId) where T : class
    {
        if (!(subscriptions[subscriptionId] is QueueSubscription<T>))
        {
            throw new InvalidOperationException($"Subscription {subscriptionId} is not for type {typeof(T)}");
        }

        var unsubscribed = subscriptions.TryRemove(subscriptionId, out var _);
        if (!unsubscribed)
        {
            throw new InvalidOperationException($"Can't remove subscription {subscriptionId}");
        }

        if (!subscriptions.Values.Any(s => s is QueueSubscription<T>))
        {
            await DoUnsubscribeAsync<T>();
        }
    }

    public async Task<QueueSubscribeResult> ReplyAsync<TMessage, TResponse>(
        Func<TMessage, QueueMessageContext, Task<TResponse>> callback)
        where TMessage : class where TResponse : class
    {
        await StartAsync();
        return await DoReplyAsync<TMessage, TResponse>((message, context, sendCallback) =>
        {
            return pipeline.ReceiveAsync(message, context, async (msg, msgContext) =>
            {
                var response = await callback(msg, msgContext);
                var result = await pipeline.PublishAsync(response, msgContext, sendCallback);
                return result.IsSuccess;
            });
        });
    }

    public Task<bool> StopReplyAsync<TMessage, TResponse>(Guid id) where TMessage : class
        where TResponse : class =>
        DoStopReplyAsync<TMessage, TResponse>(id);

    public abstract Task<(HealthStatus status, string? errorMessage)> CheckHealthAsync();


    public async Task<(TResponse message, QueueMessageContext messageContext)?> RequestAsync<TMessage, TResponse>(
        TMessage message, QueueMessageContext? parentMessageContext = null, TimeSpan? timeout = null)
        where TMessage : class
        where TResponse : class
    {
        await StartAsync();
        timeout ??= TimeSpan.FromSeconds(5);

        var messageContext = GetMessageContext<TMessage>(parentMessageContext);
        (TResponse message, QueueMessageContext context)? response = null;
        (TResponse message, QueueMessageContext messageContext)? result = null;
        var publishResult = await pipeline.PublishAsync(message, messageContext, async (_, context) =>
        {
            response = await DoRequestAsync<TMessage, TResponse>(message, context, timeout.Value);
            return new QueuePublishResult();
        });
        if (publishResult.IsSuccess && response != null)
        {
            if (!await pipeline.ReceiveAsync(response.Value.message, response.Value.context, (_, _) =>
                {
                    result = response;
                    return Task.FromResult(true);
                }))
            {
                throw new InvalidOperationException("Processing error");
            }
        }

        return result;
    }

    public async ValueTask DisposeAsync()
    {
        subscriptions.Clear();
        await DoStopAsync();
        GC.SuppressFinalize(this);
    }

    private async Task<bool> ReceiveAsync<T>(T message, QueueMessageContext context) where T : class
    {
        var queueSubscriptions = subscriptions.Values.Where(s => s is QueueSubscription<T>).ToList();

        if (!queueSubscriptions.Any())
        {
            return false;
        }

        foreach (var subscription in queueSubscriptions)
        {
            if (subscription is QueueSubscription<T> payloadSubscription)
            {
                try
                {
                    await payloadSubscription.ProcessAsync(message, context);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error while processing message {MessageType}: {ErrorText}",
                        message.GetType(),
                        ex.ToString());
                }
            }
        }

        return true;
    }

    protected Task<bool> ProcessMessageAsync<T>(T message, QueueMessageContext context,
        Func<T, QueueMessageContext, Task<bool>>? callback = null) where T : class
    {
        callback ??= ReceiveAsync;
        return pipeline.ReceiveAsync(message, context, callback);
    }

    private static QueueMessageContext GetMessageContext<T>(QueueMessageContext? parentMessageContext = null)
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

    protected abstract Task<QueuePublishResult> DoPublishAsync<T>(T message, QueueMessageContext context)
        where T : class;

    protected abstract Task<(TResponse message, QueueMessageContext context)?> DoRequestAsync<TMessage, TResponse>(
        TMessage message, QueueMessageContext context, TimeSpan timeout)
        where TMessage : class
        where TResponse : class;

    protected abstract Task<QueueSubscribeResult> DoReplyAsync<TMessage, TResponse>(
        Func<TMessage, QueueMessageContext, Func<TResponse, QueueMessageContext, Task<QueuePublishResult>>, Task<bool>>
            callback)
        where TMessage : class
        where TResponse : class;

    protected abstract Task<bool> DoStopReplyAsync<TMessage, TResponse>(Guid id)
        where TMessage : class
        where TResponse : class;

    protected abstract Task<QueueSubscribeResult> DoSubscribeAsync<T>(IQueueMessageOptions<T>? options = null)
        where T : class;

    protected abstract Task DoUnsubscribeAsync<T>() where T : class;

    private IQueueMessageOptions<T>? GetOptions<T>() where T : class =>
        messageOptions.FirstOrDefault(o => o is IQueueMessageOptions<T>) as IQueueMessageOptions<T>;

    protected virtual Task DoStartAsync() => Task.CompletedTask;

    protected virtual Task DoStopAsync() => Task.CompletedTask;

    protected async Task StartAsync()
    {
        if (!IsStarted)
        {
            await DoStartAsync();
            IsStarted = true;
        }
    }
}

