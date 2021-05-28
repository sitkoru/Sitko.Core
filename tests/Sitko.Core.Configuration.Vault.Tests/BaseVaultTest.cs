using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.Xunit;
using VaultSharp.Extensions.Configuration;
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

            application.AddVaultConfiguration(() => new VaultOptions(
                System.Environment.GetEnvironmentVariable("VAULT_URI")!,
                System.Environment.GetEnvironmentVariable("VAULT_TOKEN"), reloadOnChange: true,
                reloadCheckIntervalSeconds: 5), "Tests", "kv-v2");
            application.ConfigureServices((context, collection) =>
            {
                collection.Configure<TestConfig>(context.Configuration.GetSection("Test"));
            });
            return application;
        }
    }


    public class TestConfig
    {
        public string Foo { get; set; }
        public int Bar { get; set; }
    }
}
