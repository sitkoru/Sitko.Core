using System.Collections.Concurrent;
using Sitko.Core.App;
using Sitko.Core.Queue.Tests;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.Queue.Nats.Tests
{
    public class DeliveryTests(ITestOutputHelper testOutputHelper) : BaseTest<ManualAckQueueTestScope>(testOutputHelper)
    {
        [Fact]
        public async Task RejectedCallbackStillDeliversToOtherSubscriptionAndRedeliversToBoth()
        {
            var scope = await GetScopeAsync();
            var queue = scope.GetService<IQueue>();
            var messageId = Guid.Parse("4be94ae3-1a87-4fcf-99a6-f3bf38ffbfa5");
            var firstDeliveries = new ConcurrentQueue<Guid>();
            var secondDeliveries = new ConcurrentQueue<Guid>();
            var firstRedelivery = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondRedelivery = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var callbackCount = 0;
            var firstSubscription = await queue.SubscribeAsync<TestMessage>((message, context) =>
            {
                firstDeliveries.Enqueue(message.Id);
                if (firstDeliveries.Count == 2)
                {
                    _ = firstRedelivery.TrySetResult();
                }

                return Task.FromResult(Interlocked.Increment(ref callbackCount) != 1);
            });
            var secondSubscription = await queue.SubscribeAsync<TestMessage>((message, context) =>
            {
                secondDeliveries.Enqueue(message.Id);
                if (secondDeliveries.Count == 2)
                {
                    _ = secondRedelivery.TrySetResult();
                }

                return Task.FromResult(Interlocked.Increment(ref callbackCount) != 1);
            });
            Assert.True(firstSubscription.IsSuccess);
            Assert.True(secondSubscription.IsSuccess);
            Assert.True((await queue.PublishAsync(new TestMessage { Id = messageId })).IsSuccess);
            await Task.WhenAll(firstRedelivery.Task, secondRedelivery.Task)
                .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.Equal<Guid>([messageId, messageId], firstDeliveries);
            Assert.Equal<Guid>([messageId, messageId], secondDeliveries);
        }

        [Fact]
        public async Task RejectedCallbackRedeliversSameMessage()
        {
            var scope = await GetScopeAsync();
            var queue = scope.GetService<IQueue>();
            var messageId = Guid.Parse("dbf19532-73b5-4739-93af-a4c26435aeed");
            var deliveries = new ConcurrentQueue<Guid>();
            var firstDelivery = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var redelivery = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var deliveryCount = 0;
            var subscription = await queue.SubscribeAsync<TestMessage>((message, context) =>
            {
                deliveries.Enqueue(message.Id);
                if (Interlocked.Increment(ref deliveryCount) == 1)
                {
                    _ = firstDelivery.TrySetResult();
                    return Task.FromResult(false);
                }

                _ = redelivery.TrySetResult();
                return Task.FromResult(true);
            });
            Assert.True(subscription.IsSuccess);
            Assert.True((await queue.PublishAsync(new TestMessage { Id = messageId })).IsSuccess);
            await firstDelivery.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            var redelivered = false;
            try
            {
                await redelivery.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                redelivered = true;
            }
            catch (TimeoutException)
            {
                // Проверяем наблюдённые ID, чтобы потеря сообщения была видна в результате теста.
            }

            Assert.True(redelivered, $"Обработчик вернул false, повторная доставка не пришла. Полученные ID: {string.Join(", ", deliveries)}");
            Assert.Equal<Guid>([messageId, messageId], deliveries);
        }
    }

    public class ManualAckQueueTestScope : NatsQueueTestScope
    {
        protected override void ConfigureQueue(NatsQueueModuleOptions options, IApplicationContext applicationContext)
        {
            base.ConfigureQueue(options, applicationContext);
            options.QueueNamePrefix = $"{options.QueueNamePrefix}_{Guid.NewGuid():N}";
            options.ConfigureMessage(new NatsMessageOptions<TestMessage>
            {
                ManualAck = true,
                AckWaitInSeconds = 2,
                Durable = false
            });
        }
    }
}
