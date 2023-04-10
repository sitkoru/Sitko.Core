using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.App.Tests;

public class ConfigurationTests : BaseTest
{
    public ConfigurationTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task NestedModules()
    {
        var app = new TestApplication(Array.Empty<string>());
        app.AddModule<TestModuleFoo, TestModuleFooOptions>();
        app.AddModule<TestModuleFooBar, TestModuleFooBarOptions>();
        await app.GetServiceProviderAsync();
        var options = app.GetModulesOptions();
        options.Should().HaveCount(3);
    }

    [Theory]
    [InlineData("Baz__Foo", "123", "Baz__Bar", "456")] // all from base options
    [InlineData("Baz__Inner__Foo", "123", "Baz__Bar", "456")] // first from module, second from base
    [InlineData("Baz__Foo", "123", "Baz__Inner__Bar", "456")] // first from base, second from module
    [InlineData("Baz__Inner__Foo", "123", "Baz__Inner__Bar", "456")] // all from module options
    public async Task BaseOptions(string fooKey, string fooValue, string barKey, string barValue)
    {
        Environment.SetEnvironmentVariable(fooKey, fooValue);
        Environment.SetEnvironmentVariable(barKey, barValue);
        var app = new TestApplication(Array.Empty<string>());
        app.AddModule<TestModuleBaz, TestModuleBazOptions>();
        var sp = await app.GetServiceProviderAsync();
        var options = sp.GetRequiredService<IOptions<TestModuleBazOptions>>();
        options.Value.Foo.Should().Be(fooValue);
        options.Value.Bar.Should().Be(barValue);
    }
}

public class TestModuleFoo : BaseApplicationModule<TestModuleFooOptions>
{
    public override string OptionsKey => "Foo";
}

public class TestModuleFooBar : BaseApplicationModule<TestModuleFooBarOptions>
{
    public override string OptionsKey => "Foo:Bar";
}

public class TestModuleBaz : BaseApplicationModule<TestModuleBazOptions>
{
    public override string OptionsKey => "Baz:Inner";

    public override string[] OptionKeys => new[] { "Baz", OptionsKey };
}

public class TestModuleBazOptions : BaseModuleOptions
{
    public string Foo { get; set; } = "";
    public string Bar { get; set; } = "";
}

public class TestModuleFooBarOptions : BaseModuleOptions
{
    public string Bar { get; set; } = "";
}

public class TestModuleFooOptions : BaseModuleOptions
{
    public string Foo { get; set; } = "";
}
