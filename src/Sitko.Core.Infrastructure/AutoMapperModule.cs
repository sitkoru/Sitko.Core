using System;
using System.Collections.Generic;
using System.Reflection;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Infrastructure
{
    public class AutoMapperModule : BaseApplicationModule<AutoMapperModuleConfig>
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddAutoMapper(Config.Configure, Config.Assemblies);
        }
    }

    public class AutoMapperModuleConfig
    {
        public Action<IMapperConfigurationExpression> Configure { get; }
        public readonly List<Assembly> Assemblies = new List<Assembly> {typeof(AutoMapperModule).Assembly};

        public AutoMapperModuleConfig(Action<IMapperConfigurationExpression> configure)
        {
            Configure = configure;
        }
    }
}
