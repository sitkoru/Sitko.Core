using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App.Helpers;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.App.Tests;

public class EnvHelperTests : BaseTest
{
    private static readonly string[] RelevantEnvironmentVariableNames = [
        "DOTNET_ENVIRONMENT",
        "ASPNETCORE_ENVIRONMENT"
    ];

    public EnvHelperTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public void Default()
    {
        var env = EnvHelper.GetEnvironmentName();
        env.Should().Be(Environments.Production);
    }

    [Fact]
    public void FromEnvDoesNotPolluteProcessEnvironment()
    {
        const string sentinelName = "SITKO_ENV_HELPER_SENTINEL";
        const string sentinelValue = "sentinel";

        Environment.SetEnvironmentVariable(sentinelName, sentinelValue);

        var env = GetEnvironmentNameFromEnv("DOTNET_ENVIRONMENT", "Development");

        env.Should().Be(Environments.Development);
        Environment.GetEnvironmentVariable(sentinelName).Should().Be(sentinelValue);
    }

    [Theory]
    [InlineData("DOTNET_ENVIRONMENT", "DEVELOPMENT", "Development")]
    [InlineData("DOTNET_ENVIRONMENT", "Development", "Development")]
    [InlineData("DOTNET_environment", "Development", "Development")]
    [InlineData("ASPNETCORE_ENVIRONMENT", "PRODUCTION", "Production")]
    [InlineData("ASPNETCORE_ENVIRONMENT", "Production", "Production")]
    [InlineData("ASPNETCORE_environment", "production", "Production")]
    [InlineData("ASPNETCORE_environment", "stAgInG", "Staging")]
    public void FromEnv(string name, string value, string result)
    {
        var env = GetEnvironmentNameFromEnv(name, value);
        env.Should().Be(result);
    }

    private static string GetEnvironmentNameFromEnv(string name, string value)
    {
        var environment = Environment
            .GetEnvironmentVariables()
            .Keys
            .Cast<object>()
            .Select(key => key.ToString()!)
            .Where(IsRelevantEnvironmentVariable)
            .ToDictionary(key => key, Environment.GetEnvironmentVariable);

        try
        {
            foreach (var key in environment.Keys)
            {
                Environment.SetEnvironmentVariable(key, null);
            }

            Environment.SetEnvironmentVariable(name, value);
            return EnvHelper.GetEnvironmentName();
        }
        finally
        {
            var currentKeys = Environment
                .GetEnvironmentVariables()
                .Keys
                .Cast<object>()
                .Select(key => key.ToString()!)
                .Where(IsRelevantEnvironmentVariable);

            foreach (var key in currentKeys.Except(environment.Keys))
            {
                Environment.SetEnvironmentVariable(key, null);
            }

            foreach (var (key, originalValue) in environment)
            {
                Environment.SetEnvironmentVariable(key, originalValue);
            }
        }
    }

    private static bool IsRelevantEnvironmentVariable(string key) =>
        RelevantEnvironmentVariableNames.Any(name => string.Equals(name, key, StringComparison.OrdinalIgnoreCase));
}
