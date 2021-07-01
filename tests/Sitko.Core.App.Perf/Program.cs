using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Sitko.Core.App.Tests;

namespace Sitko.Core.App.Perf
{
    class Program
    {
        static void Main()
        {
            BenchmarkRunner.Run<LocalizationTest>();
        }
    }

    [MemoryDiagnoser]
    public class LocalizationTest
    {
        private IStringLocalizer Localizer => _factory.Create(_type);

        private IStringLocalizerFactory _factory;

        private Type _type;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var application = new LocalizationTestApplication(new string[0]);
            var host = application.GetHostBuilder().Build();
            _factory = host.Services.GetService<IStringLocalizerFactory>();
            _type = typeof(LocalizationTests);
        }

        [Benchmark]
        public IStringLocalizer CreateType() => _factory.Create(_type);

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
}
