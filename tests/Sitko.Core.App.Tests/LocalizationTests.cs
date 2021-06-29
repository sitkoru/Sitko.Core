using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Sitko.Core.App.Localization;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.App.Tests
{
    public class LocalizationTests : BaseTest<LocalizationTestScope>
    {
        public LocalizationTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            var cultureInfo = new CultureInfo("ru-RU");

            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
        }

        [Fact]
        public async Task Localize()
        {
            var scope = await GetScopeAsync();
            var localizer = scope.Get<IStringLocalizer<LocalizationTests>>();
            var localized = localizer["Bar"];
            Assert.Equal("Бар", localized);
        }

        [Fact]
        public async Task ParentFallback()
        {
            var scope = await GetScopeAsync();
            var localizer = scope.Get<IStringLocalizer<LocalizationTests>>();
            var localized = localizer["Foo"];
            Assert.Equal("Фу", localized);
        }

        [Fact]
        public async Task DefaultFallback()
        {
            var scope = await GetScopeAsync();
            var localizer = scope.Get<IStringLocalizer<LocalizationTests>>();
            var localized = localizer["Baz"];
            Assert.Equal("DefaultBaz", localized);
        }
        
        [Fact]
        public async Task NonExistent()
        {
            var scope = await GetScopeAsync();
            var localizer = scope.Get<IStringLocalizer<LocalizationTests>>();
            var localized = localizer["FooBar"];
            Assert.Equal("FooBar", localized);
        }
    }

    public class LocalizationTestScope : BaseTestScope
    {
        protected override IServiceCollection ConfigureServices(IConfiguration configuration,
            IHostEnvironment environment,
            IServiceCollection services, string name)
        {
            base.ConfigureServices(configuration, environment, services, name);
            return services.AddJsonLocalization();
        }
    }
}
