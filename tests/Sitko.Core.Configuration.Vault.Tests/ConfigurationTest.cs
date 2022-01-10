using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable VSTHRD200
namespace Sitko.Core.Configuration.Vault.Tests;

public class ConfigurationTest : BaseVaultTest
{
    public ConfigurationTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Get()
    {
        var scope = await GetScopeAsync();
        var config = scope.GetService<IOptionsMonitor<TestConfig>>();
        Assert.NotEqual(string.Empty, config.CurrentValue.Foo);
        Assert.NotEqual(0, config.CurrentValue.Bar);
    }

    [Fact]
    public async Task GetSecond()
    {
        var scope = await GetScopeAsync();
        var config = scope.GetService<IOptionsMonitor<TestConfig2>>();
        Assert.NotEqual(string.Empty, config.CurrentValue.Foo);
        Assert.NotEqual(0, config.CurrentValue.Bar);
    }

    [Fact]
    public async Task Module()
    {
        var scope = await GetScopeAsync();
        var config = scope.GetService<IOptionsMonitor<TestModuleConfig>>();
        Assert.NotEqual(string.Empty, config.CurrentValue.Foo);
        Assert.NotEqual(0, config.CurrentValue.Bar);
    }

    [Fact]
    public async Task ModuleConfigValidationFailure()
    {
        var result = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
        {
            await GetScopeAsync<VaultTestScopeWithValidationFailure>();
        });
        result.Message.Should().Contain("Bar must equals zero");
    }

    [Fact]
    public async Task AppConfigurationCheckFailure()
    {
        var result = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
        {
            await GetScopeAsync<FailingVaultTestScope>();
        });
        result.Message.Should().Contain("No data loaded from Vault secrets NonExistingSecret");
    }
}
#pragma warning restore VSTHRD200
