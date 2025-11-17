using System.Data;
using System.Data.Common;
using System.Globalization;
using DotNet.Testcontainers.Builders;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Testcontainers.ClickHouse;
using Xunit;

namespace Sitko.Core.ClickHouse.Tests;

public class ClickHouseIntegrationTests(ClickHouseFixture fixture) : IClassFixture<ClickHouseFixture>
{
    private readonly ClickHouseFixture clickHouseFixture = fixture;

    [Fact]
    public void CanWriteAndReadData()
    {
        var provider = clickHouseFixture.Provider;
        var tableName = $"test_rw_{Guid.NewGuid():N}";

        using var connection = provider.GetConnection();
        connection.Open();

        ExecuteNonQuery(provider, connection,
            $"CREATE TABLE {tableName} (Id UInt32, Name String) ENGINE = MergeTree() ORDER BY Id");

        try
        {
            ExecuteNonQuery(provider, connection,
                $"INSERT INTO {tableName} (Id, Name) VALUES (1, 'foo'), (2, 'bar')");

            using var command = provider.GetCommand($"SELECT Id, Name FROM {tableName} ORDER BY Id", connection);
            using var reader = command.ExecuteReader();

            var rows = new List<(uint Id, string Name)>();
            while (reader.Read())
            {
                var id = Convert.ToUInt32(reader.GetValue(0), CultureInfo.InvariantCulture);
                var name = reader.GetString(1);
                rows.Add((id, name));
            }

            rows.Should().Equal((1u, "foo"), (2u, "bar"));
        }
        finally
        {
            DropTable(provider, connection, tableName);
        }
    }

    [Fact]
    public void CanUseSetFinalToGetConsistentData()
    {
        var provider = clickHouseFixture.Provider;
        var tableName = $"test_final_{Guid.NewGuid():N}";
        const string olderValue = "first";
        const string newerValue = "second";

        using var connection = provider.GetConnection();
        connection.Open();

        ExecuteNonQuery(provider, connection,
            $"CREATE TABLE {tableName} (Id UInt32, Version UInt32, Value String) ENGINE = ReplacingMergeTree(Version) ORDER BY (Id)");

        try
        {
            ExecuteNonQuery(provider, connection,
                $"INSERT INTO {tableName} (Id, Version, Value) VALUES (1, 1, '{olderValue}')");
            ExecuteNonQuery(provider, connection,
                $"INSERT INTO {tableName} (Id, Version, Value) VALUES (1, 2, '{newerValue}')");
            ExecuteNonQuery(provider, connection,
                $"INSERT INTO {tableName} (Id, Version, Value) VALUES (2, 1, 'another')");

            using var finalConnection = provider.GetConnection(new Dictionary<string, string> { ["set_final"] = "1" });
            finalConnection.Open();

            var finalRows = ReadVersionedRows(provider, finalConnection,
                $"SELECT Version, Value FROM {tableName} WHERE Id = 1 ORDER BY Version");
            finalRows.Should().Equal((2u, newerValue));

            var defaultRows = ReadVersionedRows(provider, connection,
                $"SELECT Version, Value FROM {tableName} WHERE Id = 1 ORDER BY Version");
            defaultRows.Should().Equal((1u, olderValue), (2u, newerValue));
        }
        finally
        {
            DropTable(provider, connection, tableName);
        }
    }

    private static void ExecuteNonQuery(ClickHouseDbProvider provider, DbConnection connection, string sql)
    {
        using var command = provider.GetCommand(sql, connection);
        command.ExecuteNonQuery();
    }

    private static List<(uint Version, string Value)> ReadVersionedRows(ClickHouseDbProvider provider,
        DbConnection connection, string sql)
    {
        using var command = provider.GetCommand(sql, connection);
        using var reader = command.ExecuteReader();
        var rows = new List<(uint Version, string Value)>();
        while (reader.Read())
        {
            var version = Convert.ToUInt32(reader.GetValue(0), CultureInfo.InvariantCulture);
            var value = reader.GetString(1);
            rows.Add((version, value));
        }

        return rows;
    }

    private static void DropTable(ClickHouseDbProvider provider, DbConnection connection, string tableName)
    {
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        using var command = provider.GetCommand($"DROP TABLE IF EXISTS {tableName} SYNC", connection);
        command.ExecuteNonQuery();
    }
}

public sealed class ClickHouseFixture : IAsyncLifetime
{
    private ClickHouseContainer container = null!;
    private ServiceProvider? serviceProvider;
    public ClickHouseDbProvider Provider { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        const string userName = "test";
        const string password = "test";

        container = new ClickHouseBuilder()
            .WithImage("clickhouse/clickhouse-server:24.8")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request => request.ForPort(8123).ForPath("/ping")))
            .WithEnvironment("CLICKHOUSE_USER", userName)
            .WithEnvironment("CLICKHOUSE_PASSWORD", password)
            .WithEnvironment("CLICKHOUSE_DB", "default")
            .Build();
        await container.StartAsync();

        var options = new ClickHouseModuleOptions
        {
            Host = container.Hostname,
            Port = (ushort)container.GetMappedPublicPort(8123),
            Database = "default",
            UserName = userName,
            Password = password,
            Settings = { ["set_final"] = "0" }
        };

        serviceProvider = new ServiceCollection()
            .AddClickhouseClient()
            .BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        Provider = new ClickHouseDbProvider(new TestOptionsMonitor<ClickHouseModuleOptions>(options),
            httpClientFactory);

        await WaitUntilReadyAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (container != null)
        {
            await container.StopAsync(CancellationToken.None);
        }

        serviceProvider?.Dispose();
    }

    private async Task WaitUntilReadyAsync()
    {
        Exception? lastError = null;
        for (var attempt = 0; attempt < 60; attempt++)
        {
            try
            {
                using var connection = Provider.GetConnection();
                connection.Open();
                using var command = Provider.GetCommand("SELECT 1", connection);
                var result = command.ExecuteScalar();
                if (result is not null)
                {
                    return;
                }
            }
            catch (Exception exception)
            {
                lastError = exception;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new InvalidOperationException(
            $"ClickHouse container did not become ready in time. Last error: {lastError?.Message}", lastError);
    }
}

internal sealed class TestOptionsMonitor<TOptions>(TOptions current) : IOptionsMonitor<TOptions>
    where TOptions : class
{
    public TOptions CurrentValue => current;

    public TOptions Get(string? name) => current;

    public IDisposable OnChange(Action<TOptions, string?> listener) => NullDisposable.Instance;

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
