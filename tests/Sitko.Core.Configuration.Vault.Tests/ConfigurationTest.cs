﻿using FluentAssertions;
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
        var scope = await GetScopeAsync().ConfigureAwait(false);
        var config = scope.GetService<IOptionsMonitor<TestConfig>>();
        config.CurrentValue.Foo.Should().Be(scope.FirstConfig.Foo);
        config.CurrentValue.Bar.Should().Be(scope.FirstConfig.Bar);
    }

    [Fact]
    public async Task GetSecond()
    {
        var scope = await GetScopeAsync().ConfigureAwait(false);
        var config = scope.GetService<IOptionsMonitor<TestConfig2>>();
        config.CurrentValue.Foo.Should().Be(scope.SecondConfig.Foo);
        config.CurrentValue.Bar.Should().Be(scope.SecondConfig.Bar);
    }

    [Fact]
    public async Task Module()
    {
        var scope = await GetScopeAsync().ConfigureAwait(false);
        var config = scope.GetService<IOptionsMonitor<TestModuleConfig>>();
        config.CurrentValue.Foo.Should().Be(scope.FirstConfig.Foo);
        config.CurrentValue.Bar.Should().Be(scope.FirstConfig.Bar);
    }

    [Fact]
    public async Task ModuleConfigValidationFailure()
    {
        var result = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
        {
            await GetScopeAsync<VaultTestScopeWithValidationFailure>().ConfigureAwait(false);
        }).ConfigureAwait(false);
        result.Message.Should().Contain("Bar must be empty!");
    }

    [Fact]
    public async Task AppConfigurationCheckFailure()
    {
        var result = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
        {
            await GetScopeAsync<FailingVaultTestScope>().ConfigureAwait(false);
        }).ConfigureAwait(false);
        result.Message.Should().Contain("No data loaded from Vault secrets NonExistingSecret");
    }
}
#pragma warning restore VSTHRD200

