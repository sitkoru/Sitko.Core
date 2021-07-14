using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.Queue.Internal;

namespace Sitko.Core.Queue.Tests
{
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
            Func<TMessage, QueueMessageContext, PublishAsyncDelegate<TResponse>, Task<bool>> callback)
        {
            return Task.FromResult(new QueueSubscribeResult());
        }

        protected override Task<bool> DoStopReplyAsync<TMessage, TResponse>(Guid id)
        {
            return Task.FromResult(true);
        }

        protected override Task<QueueSubscribeResult> DoSubscribeAsync<T>(IQueueMessageOptions<T>? options = null)
        {
            return Task.FromResult(new QueueSubscribeResult());
        }

        protected override Task DoUnsubscribeAsync<T>()
        {
            return Task.CompletedTask;
        }

        public override Task<(HealthStatus status, string? errorMessage)> CheckHealthAsync()
        {
            return Task.FromResult((HealthStatus.Healthy, (string?)null));
        }
    }

    public class TestQueueModule : QueueModule<TestQueue, TestQueueOptions>
    {
        public override string OptionsKey => "Queue:Test";
    }

    public class TestQueueOptions : QueueModuleOptions
    {
    }
}
