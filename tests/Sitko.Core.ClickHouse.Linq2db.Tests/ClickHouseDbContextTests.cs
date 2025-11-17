using System.Data.Common;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Sitko.Core.ClickHouse.Linq2db.Tests;

public class ClickHouseDbContextTests
{
    [Fact]
    public void ContextUsesDatabaseOverride()
    {
        var options = new ClickHouseModuleOptions
        {
            Host = "db", Port = 8123, Database = "default_db", UserName = "user"
        };

        using var scope = new ClickHouseDbFactoryScope(options);
        using var context = new TestDbContext(scope.ClickHouseDbProvider, dbName: "analytics");

        var connectionOptions = GetConnectionOptions(context);
        connectionOptions.Should().NotBeNull();

        var connection = GetConnection(connectionOptions)!;
        connection.ConnectionString.Should().NotBeNull();
        connection.ConnectionString.Should().ContainEquivalentOf("analytics");
        connection.ConnectionString.Should().NotContain("default_db");
    }

    [Fact]
    public void ContextIncludesCustomSettings()
    {
        var options = new ClickHouseModuleOptions
        {
            Host = "db", Port = 8123, Database = "default_db", UserName = "user"
        };
        var settings = new Dictionary<string, string> { ["set_do_not_merge_across_partitions_select_final"] = "1" };

        using var scope = new ClickHouseDbFactoryScope(options);
        using var context = new TestDbContext(scope.ClickHouseDbProvider, settings: settings);

        var connectionOptions = GetConnectionOptions(context);
        connectionOptions.Should().NotBeNull();

        var connection = GetConnection(connectionOptions)!;
        connection.ConnectionString.Should().NotBeNull();
        connection.ConnectionString.Should().ContainEquivalentOf("set_do_not_merge_across_partitions_select_final=1");
    }

    [Fact]
    public void SetClickHouseConnectionWithoutSslUsesConnectionString()
    {
        var moduleOptions = new ClickHouseModuleOptions
        {
            Host = "plain", Port = 8123, Database = "default", UserName = "user"
        };

        using var scope = new ClickHouseDbFactoryScope(moduleOptions);
        using var context = new TestDbContext(scope.ClickHouseDbProvider);
        var connectionOptions = GetConnectionOptions(context);

        connectionOptions.Should().NotBeNull();
        var connection = GetConnection(connectionOptions)!;
        connection.ConnectionString.Should().NotBeNull();
        connection.ConnectionString.Should().ContainEquivalentOf("plain");
        connection.ConnectionString.Should().ContainEquivalentOf("default");
    }

    [Fact]
    public void SetClickHouseConnectionWithSslUsesConnectionFactory()
    {
        var moduleOptions = new ClickHouseModuleOptions
        {
            Host = "secure",
            Port = 9440,
            Database = "secure_db",
            UserName = "user",
            WithSsl = true
        };

        using var scope = new ClickHouseDbFactoryScope(moduleOptions);
        using var context = new TestDbContext(scope.ClickHouseDbProvider);

        var connectionOptions = GetConnectionOptions(context);
        connectionOptions.Should().NotBeNull();

        using var connection = GetConnection(connectionOptions);
        connection.Should().NotBeNull();
        connection.ConnectionString.Should().ContainEquivalentOf("Protocol=https");
        connection.ConnectionString.Should().ContainEquivalentOf("secure_db");
    }

    private sealed class TestDbContext : BaseClickHouseDbContext
    {
        public TestDbContext(IClickHouseDbProvider dbProvider,
            Dictionary<string, string>? settings = null, string? dbName = null)
            : base(dbProvider, settings, dbName)
        {
        }
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

    private static object? GetConnectionOptions(object source)
    {
        const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        if (source.GetType().Name.Contains("ConnectionOptions", StringComparison.OrdinalIgnoreCase))
        {
            return source;
        }

        var connectionOptionsProperty = source.GetType().GetProperty("ConnectionOptions", bindingFlags);
        if (connectionOptionsProperty != null)
        {
            return connectionOptionsProperty.GetValue(source);
        }

        var optionsProperty = source.GetType().GetProperty("Options", bindingFlags);
        if (optionsProperty?.GetValue(source) is { } optionsValue)
        {
            return GetConnectionOptions(optionsValue);
        }

        return null;
    }

    private static DbConnection? GetConnection(object connectionOptions)
    {
        const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        if (connectionOptions.GetType().GetProperty("ConnectionFactory", bindingFlags)?.GetValue(connectionOptions) is
            not Delegate factory)
        {
            if (connectionOptions.GetType().GetProperty("DbConnection", bindingFlags)?.GetValue(connectionOptions) is
                DbConnection dbConnection)
            {
                return dbConnection;
            }

            return null;
        }

        var parameters = factory.Method.GetParameters();
        return (DbConnection)(parameters.Length switch
        {
            0 => factory.DynamicInvoke()!,
            1 => factory.DynamicInvoke(connectionOptions)!,
            _ => throw new InvalidOperationException("Unexpected factory signature")
        });
    }
}
