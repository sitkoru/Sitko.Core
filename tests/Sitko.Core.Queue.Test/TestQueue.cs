using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sitko.Core.Queue.Internal;

namespace Sitko.Core.Queue.Tests
{
    public class TestQueue : BaseQueue<TestQueueConfig>
    {
        public TestQueue(TestQueueConfig config, QueueContext context, ILogger<TestQueue> logger) :
            base(config, context, logger)
        {
        }

        protected override Task<QueuePublishResult> DoPublishAsync<T>(QueuePayload<T> queuePayload)
        {
            ProcessMessageAsync(queuePayload);
            return Task.FromResult(new QueuePublishResult());
        }

        protected override Task<QueuePayload<TResponse>> DoRequestAsync<TMessage, TResponse>(
            QueuePayload<TMessage> queuePayload, TimeSpan timeout)
        {
            return Task.FromResult(new QueuePayload<TResponse>(new TResponse(), new QueueMessageContext()));
        }

        protected override Task<QueueSubscribeResult> DoReplyAsync<TMessage, TResponse>(
            Func<QueuePayload<TMessage>, Task<QueuePayload<TResponse>?>> callback)
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
    }
    
    public class TestQueueModule : QueueModule<TestQueue, TestQueueConfig>
    {
    }

    public class TestQueueConfig : QueueModuleConfig
    {
    }
}
