using AwesomeAssertions;
using Sitko.Core.Queue.Kafka.Tests.Data;
using Xunit;

namespace Sitko.Core.Queue.Kafka.Tests
{
    public class KafkaQueueRetryTest(ITestOutputHelper testOutputHelper) : BaseKafkaQueueTest(testOutputHelper)
    {
        [Fact]
        public async Task HandlerFailureRetriesSameMessageSuccessfully()
        {
            var scope = await GetScopeAsync();
            var producer = scope.GetService<IEventProducer>();
            var id = Guid.Parse("1d4fbe45-c297-4e16-8565-e53d9014c826");
            const string payload = "retry-known-payload";
            producer.Produce("retry", new RetryTestEvent { Id = id, Name = payload });

            await EventRegistrator.WaitUntilAsync(
                static () => RetryTestEventHandler.Attempts.Any(static attempt => attempt.Succeeded),
                TestContext.Current.CancellationToken);

            var attempts = RetryTestEventHandler.Attempts;
            TestOutputHelper.WriteLine($"Business attempts: {string.Join(";", attempts)}");
            _ = attempts.Take(2).Should().Equal((id, payload, false), (id, payload, true));
        }
    }
}
