using System.Data.Common;
using System.Reflection;
using FluentAssertions;
using LinqToDB;
using Xunit;

namespace Sitko.Core.ClickHouse.Linq2db.Tests;

public class ClickHouseExtensionsTests
{
    [Fact]
    public void SetClickHouseConnectionWithoutSslUsesConnectionString()
    {
        var moduleOptions = new ClickHouseModuleOptions
        {
            Host = "plain",
            Port = 8123,
            Database = "default",
            UserName = "user"
        };
        var dataOptions = new DataOptions();

        var configured = dataOptions.SetClickHouseConnection(moduleOptions);
        var connectionOptions = GetConnectionOptions(configured);

        connectionOptions.Should().NotBeNull();
        var connectionString = GetConnectionString(connectionOptions!);
        connectionString.Should().NotBeNull();
        connectionString!.Should().ContainEquivalentOf("plain");
        connectionString.Should().ContainEquivalentOf("default");
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
        var dataOptions = new DataOptions();

        var configured = dataOptions.SetClickHouseConnection(moduleOptions);

        var connectionOptions = GetConnectionOptions(configured);
        connectionOptions.Should().NotBeNull();

    using var connection = GetConnection(connectionOptions!);
    connection.Should().NotBeNull();
    connection!.ConnectionString.Should().ContainEquivalentOf("Protocol=https");
    connection.ConnectionString.Should().ContainEquivalentOf("secure_db");
    }

    private static object? GetConnectionOptions(object target)
    {
        const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        if (target.GetType().Name.Contains("ConnectionOptions", StringComparison.OrdinalIgnoreCase))
        {
            return target;
        }

        var connectionOptionsProperty = target.GetType().GetProperty("ConnectionOptions", bindingFlags);
        if (connectionOptionsProperty != null)
        {
            return connectionOptionsProperty.GetValue(target);
        }

        var optionsProperty = target.GetType().GetProperty("Options", bindingFlags);
        if (optionsProperty?.GetValue(target) is { } optionsValue)
        {
            return GetConnectionOptions(optionsValue);
        }

        return null;
    }

    private static string? GetConnectionString(object connectionOptions)
    {
        const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        return connectionOptions.GetType().GetProperty("ConnectionString", bindingFlags)?.GetValue(connectionOptions) as string;
    }

    private static DbConnection? GetConnection(object connectionOptions)
    {
        const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        if (connectionOptions.GetType().GetProperty("ConnectionFactory", bindingFlags)?.GetValue(connectionOptions) is not Delegate factory)
        {
            if (connectionOptions.GetType().GetProperty("DbConnection", bindingFlags)?.GetValue(connectionOptions) is DbConnection dbConnection)
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
