using System.Collections.Concurrent;

namespace Sitko.Core.Queue.Kafka.Tests.Data
{
    [QueueHandler("RetryTestHandlers", parallelThreadCount: 1, retryStrategy: ConsumerGroupRetryStrategy.Simple)]
    public sealed class RetryTestEventHandler : BaseMessageHandler<RetryTestEvent>
    {
        private static readonly ConcurrentQueue<(Guid Id, string Name, bool Succeeded)> RecordedAttempts = new();
        private static int attemptCount;

        internal static (Guid Id, string Name, bool Succeeded)[] Attempts => [.. RecordedAttempts];

        protected override Task Handle(RetryTestEvent message, CancellationToken cancellationToken)
        {
            var succeeded = Interlocked.Increment(ref attemptCount) > 1;
            RecordedAttempts.Enqueue((message.Id, message.Name, succeeded));
            if (!succeeded)
            {
                throw new InvalidOperationException("First business attempt fails intentionally");
            }

            return Task.CompletedTask;
        }
    }
}
