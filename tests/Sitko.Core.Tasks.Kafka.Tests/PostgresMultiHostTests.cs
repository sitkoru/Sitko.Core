using System.Net;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Sitko.Core.App;
using Sitko.Core.Db.Postgres;
using Xunit;

namespace Sitko.Core.Tasks.Kafka.Tests
{
    public sealed class PostgresMultiHostTests
    {
        [Fact]
        public async Task ModuleConnectsToSecondHostWhenFirstHostIsUnavailable()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            // Сокет без Listen удерживает недоступный порт без гонки за свободный порт.
            using var unavailable = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            unavailable.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            var unavailablePort = ((IPEndPoint)unavailable.LocalEndPoint!).Port;
            using var probe = new TcpClient();
            using var probeTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            probeTimeout.CancelAfter(TimeSpan.FromSeconds(1));
            var error = await Record.ExceptionAsync(async () =>
                await probe.ConnectAsync(IPAddress.Loopback, unavailablePort, probeTimeout.Token));
            Assert.True(error is OperationCanceledException or
                SocketException { SocketErrorCode: SocketError.ConnectionRefused or SocketError.TimedOut });
            Assert.False(probe.Connected);

            var availablePort = KafkaFixture.Postgres.GetMappedPublicPort(5432);
            var hosts = $"127.0.0.1:{unavailablePort},{KafkaFixture.Postgres.Hostname}:{availablePort}";
            var database = $"multihost_{Guid.NewGuid():N}";
            const string prefix = "Db:Postgres:MultiHostDbContext";
            var builder = Host.CreateApplicationBuilder();
            _ = builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{prefix}:Host"] = hosts,
                [$"{prefix}:Username"] = "postgres",
                [$"{prefix}:Password"] = "tasks-tests",
                [$"{prefix}:Database"] = database,
                [$"{prefix}:AutoApplyMigrations"] = "false",
                [$"{prefix}:EnableContextPooling"] = "false",
                [$"{prefix}:EnableNpgsqlPooling"] = "false",
                [$"{prefix}:ConnectionOptions:Target Session Attributes"] = "read-write",
                [$"{prefix}:ConnectionOptions:Load Balance Hosts"] = "false",
                [$"{prefix}:ConnectionOptions:Host Recheck Seconds"] = "7",
                [$"{prefix}:ConnectionOptions:Timeout"] = "2"
            });
            _ = builder.AddSitkoCore().AddPostgresDatabase<MultiHostDbContext>();
            using var host = builder.Build();
            await using var serviceScope = host.Services.CreateAsyncScope();
            var db = serviceScope.ServiceProvider.GetRequiredService<MultiHostDbContext>();
            var connection = Assert.IsType<NpgsqlConnection>(db.Database.GetDbConnection());
            var settings = new NpgsqlConnectionStringBuilder(connection.ConnectionString);
            Assert.Equal(hosts, settings.Host);
            Assert.Equal("read-write", settings.TargetSessionAttributes);
            Assert.False(settings.LoadBalanceHosts);
            Assert.Equal(7, settings.HostRecheckSeconds);
            Assert.Equal(2, settings.Timeout);
            Assert.Equal(database, settings.Database);

            try
            {
                _ = await db.Database.EnsureCreatedAsync(cancellationToken);
                await db.Database.OpenConnectionAsync(cancellationToken);
                Assert.Equal(availablePort, connection.Port);
                Assert.Equal(database, connection.Database);
                var result = await db.Database.SqlQueryRaw<int>("SELECT 42 AS \"Value\"")
                    .SingleAsync(cancellationToken);
                Assert.Equal(42, result);
            }
            finally
            {
                await db.Database.CloseConnectionAsync();
                _ = await db.Database.EnsureDeletedAsync(CancellationToken.None);
            }
        }

        public sealed class MultiHostDbContext(DbContextOptions<MultiHostDbContext> options) : DbContext(options);
    }
}
