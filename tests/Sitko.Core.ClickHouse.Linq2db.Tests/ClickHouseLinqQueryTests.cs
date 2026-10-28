using AwesomeAssertions;
using DotNet.Testcontainers.Builders;
using LinqToDB;
using Testcontainers.ClickHouse;
using Xunit;

namespace Sitko.Core.ClickHouse.Linq2db.Tests
{
    public class ClickHouseLinqQueryTests(ClickHouseLinqFixture fixture, ITestOutputHelper output) : IClassFixture<ClickHouseLinqFixture>
    {
        [Fact]
        public void LinqQueryReturnsTheMatchingRow()
        {
            output.WriteLine($"ClickHouse image clickhouse/clickhouse-server:25.8; server version {fixture.Version}");
            var provider = fixture.Provider;
            var tableName = $"test_linq_{Guid.NewGuid():N}";

            using var connection = provider.GetConnection();
            connection.Open();
            using (var create = provider.GetCommand(
                       $"CREATE TABLE {tableName} (Id UInt32, Name String) ENGINE = MergeTree() ORDER BY Id", connection))
            {
                _ = create.ExecuteNonQuery();
            }

            try
            {
                using (var insert = provider.GetCommand(
                           $"INSERT INTO {tableName} (Id, Name) VALUES (1, 'first'), (2, 'second')", connection))
                {
                    _ = insert.ExecuteNonQuery();
                }

                using var context = new TestDbContext(provider);
                var rows = context.GetTable<ClickHouseRow>().TableName(tableName)
                    .Where(row => row.Id == 2u)
                    .ToArray();

                _ = rows.Should().ContainSingle();
                _ = rows[0].Id.Should().Be(2u);
                _ = rows[0].Name.Should().Be("second");
            }
            finally
            {
                using var drop = provider.GetCommand($"DROP TABLE IF EXISTS {tableName} SYNC", connection);
                _ = drop.ExecuteNonQuery();
            }
        }

        private sealed class TestDbContext(IClickHouseDbProvider provider) : BaseClickHouseDbContext(provider);

        private sealed class ClickHouseRow
        {
            public uint Id { get; set; }
            public string Name { get; set; } = null!;
        }
    }

    public sealed class ClickHouseLinqFixture : IAsyncLifetime
    {
        private ClickHouseContainer? container;
        private ClickHouseDbFactoryScope? scope;
        public IClickHouseDbProvider Provider => scope!.ClickHouseDbProvider;
        public string Version { get; private set; } = null!;

        public async ValueTask InitializeAsync()
        {
            const string userName = "test";
            const string password = "test";
            container = new ClickHouseBuilder()
                .WithImage("clickhouse/clickhouse-server:25.8")
                .WithWaitStrategy(Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(static request => request.ForPort(8123).ForPath("/ping")))
                .WithEnvironment("CLICKHOUSE_USER", userName)
                .WithEnvironment("CLICKHOUSE_PASSWORD", password)
                .WithEnvironment("CLICKHOUSE_DB", "default")
                .Build();
            await container.StartAsync();

            scope = new ClickHouseDbFactoryScope(new ClickHouseModuleOptions
            {
                Host = container.Hostname,
                Port = container.GetMappedPublicPort(8123),
                Database = "default",
                UserName = userName,
                Password = password
            });

            using var connection = Provider.GetConnection();
            connection.Open();
            using var command = Provider.GetCommand("SELECT version()", connection);
            Version = command.ExecuteScalar()?.ToString() ?? throw new InvalidOperationException("ClickHouse did not return its version");
        }

        public async ValueTask DisposeAsync()
        {
            scope?.Dispose();
            if (container is not null)
            {
                await container.DisposeAsync();
            }
        }
    }
}
