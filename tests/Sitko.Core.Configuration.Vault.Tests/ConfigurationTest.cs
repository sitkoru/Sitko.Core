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
            Assert.Equal("444", config.CurrentValue.Foo);
            Assert.Equal(444, config.CurrentValue.Bar);
        }
    }
}
