using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    public TestConfig FirstConfig { get; } = new() { Bar = Guid.NewGuid(), Foo = Guid.NewGuid().ToString() };
    public TestConfig2 SecondConfig { get; } = new() { Bar = Guid.NewGuid(), Foo = Guid.NewGuid().ToString() };

    protected override TestApplication ConfigureApplication(TestApplication application, string name)
    {
        base.ConfigureApplication(application, name);

        application.AddVaultConfiguration();
        application.ConfigureServices((context, collection) =>
        {
            collection.Configure<TestConfig>(context.Configuration.GetSection("test"));
            collection.Configure<TestConfig2>(context.Configuration.GetSection("test2"));
        });
        application.AddModule<TestModule, TestModuleConfig>();
        return application;
    }

    public override async Task BeforeConfiguredAsync()
    {
        await base.BeforeConfiguredAsync();
        var vaultClient = new VaultClient(new VaultClientSettings(Environment.GetEnvironmentVariable("VAULT__URI"),
            new TokenAuthMethodInfo(Environment.GetEnvironmentVariable("VAULT__TOKEN"))));
        await vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(
            Environment.GetEnvironmentVariable("VAULT__SECRETS__0"),
            new { test = FirstConfig },
            mountPoint: "/secret");
        await vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(
            Environment.GetEnvironmentVariable("VAULT__SECRETS__1"),
            new { test2 = SecondConfig },
            mountPoint: "/secret");
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
