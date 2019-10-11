using System;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Infrastructure
{
    public class AutoMapperModule<T> : BaseApplicationModule<AutoMapperModuleConfig>
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddAutoMapper(Config.Configure, typeof(T).Assembly, typeof(AutoMapperModule<>).Assembly);
        }
    }

    public class AutoMapperModuleConfig
    {
        public Action<IMapperConfigurationExpression> Configure { get; }

        public AutoMapperModuleConfig(Action<IMapperConfigurationExpression> configure)
        {
            Configure = configure;
        }
    }
}
