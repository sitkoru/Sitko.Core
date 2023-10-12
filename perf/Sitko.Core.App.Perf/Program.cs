using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Sitko.Core.App.Localization;
using Sitko.Core.App.Tests;

namespace Sitko.Core.App.Perf;

public class Program
{
    public static void Main() => BenchmarkRunner.Run<LocalizationTest>();
}

[MemoryDiagnoser]
public class LocalizationTest
{
    private IStringLocalizerFactory factory;

    private Type type;
    private IStringLocalizer Localizer => factory.Create(type);

    [GlobalSetup]
    public void GlobalSetup()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddJsonLocalization(options => options.AddDefaultResource<Default>());
        var host = builder.Build();
        factory = host.Services.GetService<IStringLocalizerFactory>();
        type = typeof(LocalizationTests);
    }

    [Benchmark]
    public IStringLocalizer CreateType() => factory.Create(type);

    [Benchmark]
    public string Localize() => Localizer["Bar"];

    [Benchmark]
    public string ParentFallback() => Localizer["Foo"];

    [Benchmark]
    public string InvariantFallback() => Localizer["Baz"];

    [Benchmark]
    public string NonExistent() => Localizer["FooBar"];

    [Benchmark]
    public string DefaultLocalize() => Localizer["DefaultBar"];

    [Benchmark]
    public string DefaultParentFallback() => Localizer["DefaultFoo"];

    [Benchmark]
    public string DefaultInvariantFallback() => Localizer["DefaultBaz"];
}
