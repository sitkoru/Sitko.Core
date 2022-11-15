using System.Collections.Concurrent;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.Queue.Exceptions;
using Sitko.Core.Queue.Internal;

namespace Sitko.Core.Queue.InMemory;

public class InMemoryQueue : BaseQueue<InMemoryQueueModuleOptions>
{
    private readonly ConcurrentDictionary<Type, InMemoryQueueChannel> channels =
        new();


    public InMemoryQueue(IOptionsMonitor<InMemoryQueueModuleOptions> config, QueueContext context,
        ILogger<InMemoryQueue> logger) : base(config, context, logger)
    {
    }

    private InMemoryQueueChannel<T> GetOrCreateChannel<T>() where T : class =>
        (InMemoryQueueChannel<T>)channels.GetOrAdd(typeof(T), _ =>
        {
            var channel = new InMemoryQueueChannel<T>(Logger);
            channel.Run();
            return channel;
        });

    protected override Task<QueuePublishResult> DoPublishAsync<T>(T message, QueueMessageContext context)
    {
        var result = new QueuePublishResult();
        try
        {
            var channel = GetOrCreateChannel<T>();
            channel.Publish(message, context);
        }
        catch (Exception ex)
        {
            result.SetException(ex);
        }

        return Task.FromResult(result);
    }

    protected override async Task<(TResponse message, QueueMessageContext context)?> DoRequestAsync<TMessage,
        TResponse>(
        TMessage message, QueueMessageContext context, TimeSpan timeout)
    {
        var replyTo = Guid.NewGuid();
        var channel = GetOrCreateChannel<TResponse>();
        var resultSource = new TaskCompletionSource<(TResponse message, QueueMessageContext context)>();
        var subscriptionId = channel.Subscribe((response, responseContext) =>
        {
            if (responseContext.ReplyTo == replyTo)
            {
                resultSource.SetResult((response, responseContext));
            }

            return Task.CompletedTask;
        });
        var tasks = new List<Task> { Task.Delay(timeout), resultSource.Task };

        var sendChannel = GetOrCreateChannel<TMessage>();
        context.ReplyTo = replyTo;
        sendChannel.Publish(message, context);

        await Task.WhenAny(tasks);
        channel.UnSubscribe(subscriptionId);
        if (!resultSource.Task.IsCompleted)
        {
            throw new QueueRequestTimeoutException(timeout);
        }

        return resultSource.Task.Result;
    }

    protected override Task<QueueSubscribeResult> DoReplyAsync<TMessage, TResponse>(
        Func<TMessage, QueueMessageContext, Func<TResponse, QueueMessageContext, Task<QueuePublishResult>>, Task<bool>>
            callback)
    {
        var result = new QueueSubscribeResult();
        try
        {
            var channel = GetOrCreateChannel<TMessage>();
            result.SubscriptionId = channel.Subscribe(async (request, context) =>
            {
                if (context.ReplyTo != null)
                {
                    await callback(request, context, DoPublishAsync);
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
            channel.Subscribe((message, context) =>
                context.ReplyTo == null ? ProcessMessageAsync(message, context) : Task.CompletedTask);
        }
        catch (Exception ex)
        {
            result.SetException(ex);
        }

        return Task.FromResult(result);
    }

    protected override async Task DoUnsubscribeAsync<T>()
    {
        if (channels.TryRemove(typeof(T), out var channel))
        {
            await channel.StopAsync();
        }
    }

    protected override async Task DoStopAsync()
    {
        await base.DoStopAsync();
        foreach (var channel in channels.Values)
        {
            await channel.StopAsync();
        }

        channels.Clear();
    }

    public override Task<(HealthStatus status, string? errorMessage)> CheckHealthAsync() =>
        Task.FromResult((HealthStatus.Healthy, (string?)null));
}

