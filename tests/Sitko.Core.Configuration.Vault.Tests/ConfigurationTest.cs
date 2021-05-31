using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Configuration.Vault.Tests
{
    public class ConfigurationTest : BaseVaultTest
    {
        public ConfigurationTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Get()
        {
            var scope = await GetScopeAsync();
            var config = scope.Get<IOptionsMonitor<TestConfig>>();
            Assert.NotEqual(string.Empty, config.CurrentValue.Foo);
            Assert.NotEqual(0, config.CurrentValue.Bar);
        }

        [Fact]
        public async Task Module()
        {
            var scope = await GetScopeAsync();
            var config = scope.Get<IOptionsMonitor<TestModuleConfig>>();
            Assert.NotEqual(string.Empty, config.CurrentValue.Foo);
            Assert.NotEqual(0, config.CurrentValue.Bar);
        }

        [Fact]
        public async Task ModuleConfigValidationFailure()
        {
            var scope = await GetScopeAsync<VaultTestScopeWithValidationFailure>();
            Assert.Throws<OptionsValidationException>(() =>
            {
                var config = scope.Get<IOptions<TestModuleWithValidationConfig>>();
                Assert.Equal(0, config.Value.Bar);
            });
        }
    }
}
