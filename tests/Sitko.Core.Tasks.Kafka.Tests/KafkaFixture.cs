using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.Kafka;
using Xunit;

[assembly: AssemblyFixture(typeof(Sitko.Core.Tasks.Kafka.Tests.KafkaFixture))]

namespace Sitko.Core.Tasks.Kafka.Tests
{
    public sealed class KafkaFixture : IAsyncLifetime
    {
        public static KafkaContainer Kafka { get; private set; } = null!;
        public static IContainer Postgres { get; private set; } = null!;

        public async ValueTask InitializeAsync()
        {
            Kafka = new KafkaBuilder().WithImage("confluentinc/cp-kafka:7.4.0").Build();
            Postgres = new ContainerBuilder().WithImage("postgres:17-alpine")
                .WithEnvironment("POSTGRES_PASSWORD", "tasks-tests")
                .WithPortBinding(5432, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready", "-h", "127.0.0.1", "-U", "postgres"))
                .Build();
            try
            {
                await Postgres.StartAsync(TestContext.Current.CancellationToken);
                await Kafka.StartAsync(TestContext.Current.CancellationToken);
            }
            catch
            {
                await DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await Kafka.DisposeAsync();
            }
            finally
            {
                await Postgres.DisposeAsync();
            }
        }
    }
}
