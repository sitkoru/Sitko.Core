using System;
using System.Threading.Tasks;
using FluentAssertions;
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
        options.Should().HaveCount(2);
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

public class TestModuleFooBarOptions : BaseModuleOptions
{
    public string Bar { get; set; }
}

public class TestModuleFooOptions : BaseModuleOptions
{
    public string Foo { get; set; }
}
