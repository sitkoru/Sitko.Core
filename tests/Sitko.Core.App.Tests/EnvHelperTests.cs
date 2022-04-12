using System;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App.Helpers;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.App.Tests;

public class EnvHelperTests : BaseTest
{
    public EnvHelperTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public void Default()
    {
        var env = EnvHelper.GetEnvironmentName();
        env.Should().Be(Environments.Production);
    }

    [Theory]
    [InlineData("DOTNET_ENVIRONMENT", "DEVELOPMENT", "Development")]
    [InlineData("DOTNET_ENVIRONMENT", "Development", "Development")]
    [InlineData("DOTNET_environment", "Development", "Development")]
    [InlineData("ASPNET_ENVIRONMENT", "PRODUCTION", "Production")]
    [InlineData("ASPNET_ENVIRONMENT", "Production", "Production")]
    [InlineData("ASPNET_environment", "production", "Production")]
    [InlineData("ASPNET_environment", "stAgInG", "Staging")]
    public void FromEnv(string name, string value, string result)
    {
        foreach (var key in Environment.GetEnvironmentVariables().Keys)
        {
            Environment.SetEnvironmentVariable(key.ToString()!, null);
        }

        Environment.SetEnvironmentVariable(name, value);
        var env = EnvHelper.GetEnvironmentName();
        env.Should().Be(result);
    }
}
