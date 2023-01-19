using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.Xunit;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using Xunit.Abstractions;

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

    protected override TestApplication ConfigureApplication(TestApplication application, string name)
    {
        base.ConfigureApplication(application, name);

        application.AddVaultConfiguration(options =>
        {
            options.Secrets = new List<string> { firstSecretId.ToString(), secondSecretId.ToString() };
        });
        application.ConfigureServices((context, collection) =>
        {
            collection.Configure<TestConfig>(context.Configuration.GetSection("test"));
            collection.Configure<TestConfig2>(context.Configuration.GetSection("test2"));
        });
        application.AddModule<TestModule, TestModuleConfig>();
        return application;
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
}

public class FailingVaultTestScope : BaseTestScope
{
    protected override TestApplication ConfigureApplication(TestApplication application, string name)
    {
        base.ConfigureApplication(application, name);
        application.AddVaultConfiguration(options =>
        {
            options.Secrets = new List<string> { "NonExistingSecret" };
        });
        return application;
    }
}

public class VaultTestScopeWithValidationFailure : VaultTestScope
{
    protected override TestApplication ConfigureApplication(TestApplication application, string name)
    {
        base.ConfigureApplication(application, name);
        application.AddModule<TestModuleWithValidation, TestModuleWithValidationConfig>();
        return application;
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

