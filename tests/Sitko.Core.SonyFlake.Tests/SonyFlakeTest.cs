using System;
using System.Threading.Tasks;
using Sitko.Core.App;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.SonyFlake.Tests
{
    public class SonyFlakeTest : BaseTest<SonyFlakeTestScope>
    {
        public SonyFlakeTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Generate()
        {
            var scope = GetScope();

            var provider = scope.Get<IIdProvider>();

            var id = await provider.NextAsync();

            Assert.True(id > 0);
        }
    }

    public class SonyFlakeTestScope : BaseTestScope
    {
        protected override Application ConfigureApplication(Application application, string name)
        {
            return base.ConfigureApplication(application, name).AddModule<SonyFlakeModule, SonyFlakeModuleConfig>(
                (configuration, environment) => new SonyFlakeModuleConfig(new Uri(configuration["SONYFLAKE_URI"])));
        }
    }
}
