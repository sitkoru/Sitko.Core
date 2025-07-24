using System.Globalization;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App.Localization;
using Sitko.Core.Xunit;
using Xunit;

// ReSharper disable UnusedTypeParameter

namespace Sitko.Core.App.Tests;

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
        var provider = scope.GetService<ILocalizationProvider<LocalizationTests>>();
        var localized = provider["Bar"];
        Assert.Equal("Бар", localized);
    }

    [Fact]
    public async Task LocalizeParameters()
    {
        var scope = await GetScopeAsync();
        var provider = scope.GetService<ILocalizationProvider<LocalizationTests>>();
        var localized = provider["Bar: {0}", 0];
        Assert.Equal("Бар: 0", localized);
    }

    [Fact]
    public async Task NoLocalizer()
    {
        var scope = await GetScopeAsync<NoLocalizationTestScope>();
        var provider = scope.GetService<ILocalizationProvider<LocalizationTests>>();
        var localized = provider["Bar"];
        Assert.Equal("Bar", localized);
    }

    [Fact]
    public async Task NoLocalizerParameters()
    {
        var scope = await GetScopeAsync<NoLocalizationTestScope>();
        var provider = scope.GetService<ILocalizationProvider<LocalizationTests>>();
        var localized = provider["Bar: {0}", 0];
        Assert.Equal("Bar: 0", localized);
    }

    [Fact]
    public async Task ParentFallback()
    {
        var scope = await GetScopeAsync();
        var provider = scope.GetService<ILocalizationProvider<LocalizationTests>>();
        var localized = provider["Foo"];
        Assert.Equal("Фу", localized);
    }

    [Fact]
    public async Task InvariantFallback()
    {
        var scope = await GetScopeAsync();
        var provider = scope.GetService<ILocalizationProvider<LocalizationTests>>();
        var localized = provider["Baz"];
        Assert.Equal("DefaultBaz", localized);
    }

    [Fact]
    public async Task DefaultLocalize()
    {
        var scope = await GetScopeAsync();
        var provider = scope.GetService<ILocalizationProvider<LocalizationTests>>();
        var localized = provider["DefaultBar"];
        Assert.Equal("БарБар", localized);
    }

    [Fact]
    public async Task DefaultParentFallback()
    {
        var scope = await GetScopeAsync();
        var provider = scope.GetService<ILocalizationProvider<LocalizationTests>>();
        var localized = provider["DefaultFoo"];
        Assert.Equal("ФуФу", localized);
    }

    [Fact]
    public async Task DefaultInvariantFallback()
    {
        var scope = await GetScopeAsync();
        var provider = scope.GetService<ILocalizationProvider<LocalizationTests>>();
        var localized = provider["DefaultBaz"];
        Assert.Equal("Default", localized);
    }

    [Fact]
    public async Task DefaultNonExistent()
    {
        var scope = await GetScopeAsync();
        var provider = scope.GetService<ILocalizationProvider<LocalizationTests>>();
        var localized = provider["DefaultFooBar"];
        Assert.Equal("DefaultFooBar", localized);
    }

    [Fact]
    public async Task NonExistent()
    {
        var scope = await GetScopeAsync();
        var provider = scope.GetService<ILocalizationProvider<LocalizationTests>>();
        var localized = provider["FooBar"];
        Assert.Equal("FooBar", localized);
    }

    [Fact]
    public async Task Generic()
    {
        var scope = await GetScopeAsync();
        var provider = scope.GetService<ILocalizationProvider<LocalizationTests<string>>>();
        var localized = provider["Bar"];
        Assert.Equal("Бар", localized);
    }

    [Fact]
    public async Task GenericMultipleParameters()
    {
        var scope = await GetScopeAsync();
        var provider = scope.GetService<ILocalizationProvider<LocalizationTests<string, double>>>();
        var localized = provider["Bar"];
        Assert.Equal("Бар", localized);
    }

    [Fact]
    public async Task GenericInterface()
    {
        var scope = await GetScopeAsync();
        var provider = scope.GetService<ILocalizationProvider<ILocalizationTests<string, double, int>>>();
        var localized = provider["Bar"];
        Assert.Equal("Бар", localized);
    }
}

public class LocalizationTests<T>;

public class LocalizationTests<T, T2>;

// ReSharper disable once InconsistentNaming
public interface ILocalizationTests<T, T2, T3>;

public class Default;

public class LocalizationTestScope : BaseTestScope
{
    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name)
    {
        base.ConfigureApplication(hostBuilder, name)
            .GetSitkoCore()
            .AddJsonLocalization(options => options.AddDefaultResource<Default>());
        return hostBuilder;
    }
}

public class NoLocalizationTestScope : BaseTestScope;
