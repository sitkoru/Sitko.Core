using ClickHouse.Driver.ADO;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Sitko.Core.ClickHouse.Tests;

public class ClickHouseDbProviderTests
{
    [Fact]
    public void GetConnectionUsesCurrentOptions()
    {
        var options = new ClickHouseModuleOptions
        {
            Host = "db.internal",
            Port = 8443,
            Database = "telemetry",
            UserName = "bot",
            Password = "pwd",
            WithSsl = true
        };
        var monitor = new StaticOptionsMonitor(options);
        using var httpClientScope = new HttpClientFactoryScope();
        var provider = new ClickHouseDbProvider(monitor, httpClientScope.Factory);

        using var connection = provider.GetConnection();

        connection.Should().BeOfType<ClickHouseConnection>();
        connection.ConnectionString.Should().ContainEquivalentOf("db.internal");
        connection.ConnectionString.Should().ContainEquivalentOf("8443");
        connection.ConnectionString.Should().ContainEquivalentOf("Protocol=https");
    }

    [Fact]
    public void GetCommandReturnsConfiguredClickHouseCommand()
    {
        var options = new ClickHouseModuleOptions
        {
            Host = "localhost", Port = 8123, Database = "default", UserName = "user"
        };
        var monitor = new StaticOptionsMonitor(options);
        using var httpClientScope = new HttpClientFactoryScope();
        var provider = new ClickHouseDbProvider(monitor, httpClientScope.Factory);
        using var connection = provider.GetConnection();

        using var command = provider.GetCommand("SELECT 1", connection);

        command.Should().BeOfType<ClickHouseCommand>();
        command.CommandText.Should().Be("SELECT 1");
        command.Connection.Should().Be(connection);
    }

    private sealed class StaticOptionsMonitor(ClickHouseModuleOptions current)
        : IOptionsMonitor<ClickHouseModuleOptions>
    {
        public ClickHouseModuleOptions CurrentValue => current;

        public ClickHouseModuleOptions Get(string? name) => current;

        public IDisposable OnChange(Action<ClickHouseModuleOptions, string?> listener) => NullDisposable.Instance;

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
