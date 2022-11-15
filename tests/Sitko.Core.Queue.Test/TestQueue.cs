using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.Queue.Internal;

namespace Sitko.Core.Queue.Tests;

public class TestQueue : BaseQueue<TestQueueOptions>
{
    public TestQueue(IOptionsMonitor<TestQueueOptions> config, QueueContext context, ILogger<TestQueue> logger) :
        base(config, context, logger)
    {
    }

    protected override async Task<QueuePublishResult> DoPublishAsync<T>(T message, QueueMessageContext context)
    {
        await ProcessMessageAsync(message, context);
        return new QueuePublishResult();
    }

    protected override Task<(TResponse message, QueueMessageContext context)?> DoRequestAsync<TMessage, TResponse>(
        TMessage message, QueueMessageContext context, TimeSpan timeout)
    {
        (TResponse message, QueueMessageContext context)? result = (Activator.CreateInstance<TResponse>(),
            new QueueMessageContext());
        return Task.FromResult(result);
    }

    protected override Task<QueueSubscribeResult> DoReplyAsync<TMessage, TResponse>(
        Func<TMessage, QueueMessageContext, Func<TResponse, QueueMessageContext, Task<QueuePublishResult>>, Task<bool>>
            callback) =>
        Task.FromResult(new QueueSubscribeResult());

    protected override Task<bool> DoStopReplyAsync<TMessage, TResponse>(Guid id) => Task.FromResult(true);

    protected override Task<QueueSubscribeResult> DoSubscribeAsync<T>(IQueueMessageOptions<T>? options = null) =>
        Task.FromResult(new QueueSubscribeResult());

    protected override Task DoUnsubscribeAsync<T>() => Task.CompletedTask;

    public override Task<(HealthStatus status, string? errorMessage)> CheckHealthAsync() =>
        Task.FromResult((HealthStatus.Healthy, (string?)null));
}

public class TestQueueModule : QueueModule<TestQueue, TestQueueOptions>
{
    public override string OptionsKey => "Queue:Test";
}

public class TestQueueOptions : QueueModuleOptions
{
}

