using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Xunit;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using Xunit;

namespace Sitko.Core.Configuration.Vault.Tests;

public abstract class BaseVaultTest : BaseTest<VaultTestScope>
{
    protected BaseVaultTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }
}

public class VaultTestScope : BaseTestScope
{
    private readonly Guid firstSecretId = Guid.NewGuid();
    private readonly Guid secondSecretId = Guid.NewGuid();
    private readonly VaultClient vaultClient;

    public VaultTestScope() =>
        vaultClient = new VaultClient(new VaultClientSettings(Environment.GetEnvironmentVariable("VAULT__URI"),
            new TokenAuthMethodInfo(Environment.GetEnvironmentVariable("VAULT__TOKEN"))));

    public TestConfig FirstConfig { get; } = new() { Bar = Guid.NewGuid(), Foo = Guid.NewGuid().ToString() };
    public TestConfig2 SecondConfig { get; } = new() { Bar = Guid.NewGuid(), Foo = Guid.NewGuid().ToString() };

    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name)
    {
        base.ConfigureApplication(hostBuilder, name)
            .AddVaultConfiguration(options =>
            {
                options.Secrets = [firstSecretId.ToString(), secondSecretId.ToString()];
                options.ReloadCheckIntervalSeconds = 1;
                options.ReloadOnChange = true;
            });
        hostBuilder.Services.Configure<TestConfig>(hostBuilder.Configuration.GetSection("test"));
        hostBuilder.Services.Configure<TestConfig2>(hostBuilder.Configuration.GetSection("test2"));
        hostBuilder.GetSitkoCore().AddModule<TestModule, TestModuleConfig>();
        return hostBuilder;
    }

    public override async Task BeforeConfiguredAsync(string name)
    {
        await base.BeforeConfiguredAsync(name).ConfigureAwait(false);

        await vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(
            firstSecretId.ToString(),
            new { test = FirstConfig },
            mountPoint: "/secret").ConfigureAwait(false);
        await vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(
            secondSecretId.ToString(),
            new { test2 = SecondConfig },
            mountPoint: "/secret").ConfigureAwait(false);
    }

    protected override async Task OnDisposeAsync()
    {
        await base.OnDisposeAsync().ConfigureAwait(false);
        await vaultClient.V1.Secrets.KeyValue.V2.DeleteMetadataAsync(firstSecretId.ToString(), "/secret")
            .ConfigureAwait(false);
        await vaultClient.V1.Secrets.KeyValue.V2.DeleteMetadataAsync(secondSecretId.ToString(), "/secret")
            .ConfigureAwait(false);
    }

    public async Task<TestConfig> UpdateConfigAsync()
    {
        var changedConfig = new TestConfig { Bar = Guid.NewGuid(), Foo = Guid.NewGuid().ToString() };
        await vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(
            firstSecretId.ToString(),
            new { test = changedConfig },
            mountPoint: "/secret").ConfigureAwait(false);
        return changedConfig;
    }
}

public class FailingVaultTestScope : BaseTestScope
{
    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name) =>
        base.ConfigureApplication(hostBuilder, name)
            .AddVaultConfiguration(options =>
            {
                options.Secrets = new List<string> { "NonExistingSecret" };
            });

    public override async Task OnCreatedAsync()
    {
        await base.OnCreatedAsync();
        await StartApplicationAsync(TestContext.Current.CancellationToken);
    }
}

public class VaultTestScopeWithValidationFailure : VaultTestScope
{
    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name)
    {
        base.ConfigureApplication(hostBuilder, name);
        hostBuilder.GetSitkoCore().AddModule<TestModuleWithValidation, TestModuleWithValidationConfig>();
        return hostBuilder;
    }
}

public class TestConfig
{
    public string Foo { get; set; } = "";
    public Guid Bar { get; set; }
}

public class TestConfig2
{
    public string Foo { get; set; } = "";
    public Guid Bar { get; set; }
}

public class TestModule : BaseApplicationModule<TestModuleConfig>
{
    public override string OptionsKey => "Test";
}

public class TestModuleConfig : BaseModuleOptions
{
    public string Foo { get; set; } = "";
    public Guid Bar { get; set; }
}

public class TestModuleWithValidation : BaseApplicationModule<TestModuleWithValidationConfig>
{
    public override string OptionsKey => "Test";
}

public class TestModuleWithValidationConfig : BaseModuleOptions
{
    public string Foo { get; set; } = "";
    public Guid Bar { get; set; }
}

public class TestModuleWithValidationConfigValidator : AbstractValidator<TestModuleWithValidationConfig>
{
    public TestModuleWithValidationConfigValidator() =>
        RuleFor(o => o.Bar).Empty().WithMessage("Bar must be empty!");
}
