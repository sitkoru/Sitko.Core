using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App.Web;
using Sitko.Core.Xunit;

namespace Sitko.Core.ElasticStack.Tests
{
    public class ElasticStackScope : BaseTestScope<ElasticApplication>
    {
    }

    public class ElasticStartup : BaseStartup
    {
        public ElasticStartup(IConfiguration configuration, IHostEnvironment environment) : base(configuration,
            environment)
        {
        }
    }

    public class ElasticApplication : WebApplication<ElasticStartup>
    {
        public ElasticApplication(string[] args) : base(args)
        {
            AddModule<ElasticStackModule, ElasticStackModuleOptions>();
        }
    }
}
