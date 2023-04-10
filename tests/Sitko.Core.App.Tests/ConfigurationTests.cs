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


    public static IEnumerable<object[]> BaseOptionsData =>
        new List<object[]>
        {
            new object[]
            {
                "Baz__Foo", Guid.NewGuid().ToString(), "Baz__Bar", Guid.NewGuid().ToString()
            }, // all from base options
            new object[]
            {
                "Baz__Inner__Foo", Guid.NewGuid().ToString(), "Baz__Bar", Guid.NewGuid().ToString()
            }, // first from module, second from base
            new object[]
            {
                "Baz__Foo", Guid.NewGuid().ToString(), "Baz__Inner__Bar", Guid.NewGuid().ToString()
            }, // first from base, second from module
            new object[]
            {
                "Baz__Inner__Foo", Guid.NewGuid().ToString(), "Baz__Inner__Bar", Guid.NewGuid().ToString()
            }, // all from module options
        };

    [Theory]
    [MemberData(nameof(BaseOptionsData))]
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
        Environment.SetEnvironmentVariable(fooKey,"");
        Environment.SetEnvironmentVariable(barKey, "");
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
