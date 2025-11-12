using ClickHouse.Driver.ADO;
using FluentAssertions;
using Xunit;

namespace Sitko.Core.ClickHouse.Tests;

public class ClickHouseExtensionsTests
{
    [Fact]
    public void GetDbConnectionReturnsConnectionWithExpectedConnectionString()
    {
        var options = new ClickHouseModuleOptions
        {
            Host = "test-host",
            Port = 9000,
            Database = "metrics",
            UserName = "metrics_user",
            Password = "pwd",
            Settings = new Dictionary<string, string>
            {
                ["compression"] = "gzip"
            }
        };

        using var connection = options.GetDbConnection();

        connection.Should().BeOfType<ClickHouseConnection>();
    connection.ConnectionString.Should().ContainEquivalentOf("test-host");
    connection.ConnectionString.Should().ContainEquivalentOf("metrics");
    connection.ConnectionString.Should().ContainEquivalentOf("Protocol=http");
    }

    [Fact]
    public void GetDbConnectionWithSslUsesHttpsProtocol()
    {
        var options = new ClickHouseModuleOptions
        {
            Host = "secure-host",
            Port = 9440,
            Database = "secure",
            UserName = "secure_user",
            Password = "pwd",
            WithSsl = true
        };

        using var connection = options.GetDbConnection();

        connection.ConnectionString.Should().ContainEquivalentOf("Protocol=https");
    }
}
