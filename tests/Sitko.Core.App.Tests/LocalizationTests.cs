using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            var provider = scope.Get<ILocalizationProvider<LocalizationTests>>();
            var localized = provider["Bar"];
            Assert.Equal("Бар", localized);
        }

        [Fact]
        public async Task ParentFallback()
        {
            var scope = await GetScopeAsync();
            var provider = scope.Get<ILocalizationProvider<LocalizationTests>>();
            var localized = provider["Foo"];
            Assert.Equal("Фу", localized);
        }

        [Fact]
        public async Task DefaultFallback()
        {
            var scope = await GetScopeAsync();
            var provider = scope.Get<ILocalizationProvider<LocalizationTests>>();
            var localized = provider["Baz"];
            Assert.Equal("DefaultBaz", localized);
        }

        [Fact]
        public async Task NonExistent()
        {
            var scope = await GetScopeAsync();
            var provider = scope.Get<ILocalizationProvider<LocalizationTests>>();
            var localized = provider["FooBar"];
            Assert.Equal("FooBar", localized);
        }

        [Fact]
        public async Task Generic()
        {
            var scope = await GetScopeAsync();
            var provider = scope.Get<ILocalizationProvider<LocalizationTests<string>>>();
            var localized = provider["Bar"];
            Assert.Equal("Бар", localized);
        }
        
        [Fact]
        public async Task GenericMultipleParameters()
        {
            var scope = await GetScopeAsync();
            var provider = scope.Get<ILocalizationProvider<LocalizationTests<string, double>>>();
            var localized = provider["Bar"];
            Assert.Equal("Бар", localized);
        }
        
        [Fact]
        public async Task GenericInterface()
        {
            var scope = await GetScopeAsync();
            var provider = scope.Get<ILocalizationProvider<LocalizationTests<string, double, int>>>();
            var localized = provider["Bar"];
            Assert.Equal("Бар", localized);
        }
    }

    public class LocalizationTests<T>
    {
    }

    public class LocalizationTests<T, T2>
    {
    }

    public interface LocalizationTests<T, T2, T3>
    {
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
