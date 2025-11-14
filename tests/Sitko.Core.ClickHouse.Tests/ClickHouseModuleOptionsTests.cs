using FluentAssertions;
using Xunit;

namespace Sitko.Core.ClickHouse.Tests;

public class ClickHouseModuleOptionsTests
{
    private static ClickHouseModuleOptions CreateOptions() => new()
    {
        Host = "click.example.com",
        Port = 9440,
        UserName = "service",
        Password = "secret",
        Database = "analytics",
        UseSession = true,
        Settings = new Dictionary<string, string>
        {
            ["custom_setting"] = "42"
        }
    };

    [Fact]
    public void GetConnectionCombinesAllSettings()
    {
        var options = CreateOptions();
        var overrideSettings = new Dictionary<string, string>
        {
            ["custom_setting"] = "24",
            ["compression"] = "gzip"
        };

        var builder = options.GetConnection(overrideSettings, "override_db");

        builder.Host.Should().Be(options.Host);
        builder.Port.Should().Be(options.Port);
        builder.Database.Should().Be("override_db");
        builder.Username.Should().Be(options.UserName);
        builder.Password.Should().Be(options.Password);
        builder.UseSession.Should().BeTrue();
        builder["custom_setting"].Should().Be("24");
        builder["compression"].Should().Be("gzip");
    }

    [Fact]
    public void GetConnectionWithSslUsesHttpsProtocol()
    {
        var options = CreateOptions();
        options.WithSsl = true;

        var builder = options.GetConnection();

        builder.Protocol.Should().Be("https");
    }

    [Fact]
    public void GetConnectionStringMatchesBuilder()
    {
        var options = CreateOptions();
        options.Settings["compression"] = "br";

        var builder = options.GetConnection();
        var connectionString = options.GetConnectionString();

        connectionString.Should().Be(builder.ConnectionString);
    }
}
