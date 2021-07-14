using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Configuration.Vault.Tests
{
    public abstract class BaseVaultTest : BaseTest<VaultTestScope>
    {
        protected BaseVaultTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }

    public class VaultTestScope : BaseTestScope
    {
        protected override TestApplication ConfigureApplication(TestApplication application, string name)
        {
            base.ConfigureApplication(application, name);

            application.AddVaultConfiguration();
            application.ConfigureServices((_, context, collection) =>
            {
                collection.Configure<TestConfig>(context.Configuration.GetSection("test"));
            });
            application.AddModule<TestModule, TestModuleConfig>();
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
        public string Foo { get; set; }
        public int Bar { get; set; }
    }

    public class TestModule : BaseApplicationModule<TestModuleConfig>
    {
        public override string OptionsKey => "Test";
    }

    public class TestModuleConfig : BaseModuleOptions
    {
        public string Foo { get; set; }
        public int Bar { get; set; }
    }

    public class TestModuleWithValidation : BaseApplicationModule<TestModuleWithValidationConfig>
    {
        public override string OptionsKey => "Test";
    }

    public class TestModuleWithValidationConfig : BaseModuleOptions
    {
        public string Foo { get; set; }
        public int Bar { get; set; }
    }

    public class TestModuleWithValidationConfigValidator : AbstractValidator<TestModuleWithValidationConfig>
    {
        public TestModuleWithValidationConfigValidator()
        {
            RuleFor(o => o.Bar).Equal(0).WithMessage("Bar must equals zero!");
        }
    }
}
