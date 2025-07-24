using Testcontainers.Kafka;
using Xunit;

namespace Sitko.Core.Queue.Kafka.Tests;

public class KafkaFixture : IAsyncLifetime
{
    public static KafkaContainer KafkaContainer { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        KafkaContainer = new KafkaBuilder().WithImage("confluentinc/cp-kafka:7.4.0").Build();

        await KafkaContainer.StartAsync()
            .ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync() => await KafkaContainer.StopAsync(CancellationToken.None);
}
