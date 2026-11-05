using Sitko.Core.App;
using Sitko.Core.Queue.Tests;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.Queue.InMemory.Tests
{
    public class MiddlewareTests(ITestOutputHelper testOutputHelper) : BaseTest(testOutputHelper)
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task PublishWithoutSubscriptionsDoesNotInvokeReceiveMiddleware(bool unsubscribe)
        {
            var scope = await GetScopeAsync<MiddlewareQueueTestScope>();
            var queue = scope.GetService<IQueue>();
            var middleware = scope.GetService<ReceiveEntryMiddleware>();
            var reply = await queue.ReplyAsync<TestMessage, TestResponse>(static (message, _) =>
                Task.FromResult(new TestResponse { Id = message.Id }));
            Assert.True(reply.IsSuccess);

            if (unsubscribe)
            {
                var subscription = await queue.SubscribeAsync<TestMessage>(static (_, _) => Task.FromResult(true));
                Assert.True(subscription.IsSuccess);
                Assert.True((await queue.PublishAsync(new TestMessage())).IsSuccess);
                await DrainAsync();
                Assert.Equal(1, middleware.Received);
                await queue.UnsubscribeAsync<TestMessage>(subscription.SubscriptionId);
            }

            Assert.True((await queue.PublishAsync(new TestMessage())).IsSuccess);
            await DrainAsync();

            Assert.Equal(unsubscribe ? 1 : 0, middleware.Received);
            Assert.True(await queue.StopReplyAsync<TestMessage, TestResponse>(reply.SubscriptionId));
            return;

            async Task DrainAsync()
            {
                // Запрос того же типа проходит FIFO после обычной публикации.
                var marker = new TestMessage();
                var response = await queue.RequestAsync<TestMessage, TestResponse>(marker,
                    timeout: TimeSpan.FromSeconds(10));
                _ = Assert.NotNull(response);
                Assert.Equal(marker.Id, response.Value.message.Id);
            }
        }
    }

    public class MiddlewareQueueTestScope : InMemoryQueueTestScope
    {
        protected override void Configure(IApplicationContext applicationContext,
            InMemoryQueueModuleOptions options, string name) => options.RegisterMiddleware<ReceiveEntryMiddleware>();
    }

    public class ReceiveEntryMiddleware : BaseQueueMiddleware
    {
        private int received;

        public int Received => Volatile.Read(ref received);

        public override Task<bool> ReceiveAsync<T>(T message, QueueMessageContext messageContext,
            Func<T, QueueMessageContext, Task<bool>>? callback = null)
        {
            if (messageContext.ReplyTo == null)
            {
                _ = Interlocked.Increment(ref received);
            }

            return base.ReceiveAsync(message, messageContext, callback);
        }
    }
}
