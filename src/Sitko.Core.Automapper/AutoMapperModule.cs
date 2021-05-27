using System;
using System.Collections.Generic;
using System.Reflection;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Automapper
{
    public class AutoMapperModule : BaseApplicationModule<AutoMapperModuleConfig>
    {
        public override string GetConfigKey()
        {
            return "AutoMapper";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            AutoMapperModuleConfig startupConfig)
        {
            base.ConfigureServices(context, services, startupConfig);
            services.AddAutoMapper(startupConfig.Configure, startupConfig.Assemblies);
        }
    }

    public class AutoMapperModuleConfig : BaseModuleConfig
    {
        public Action<IMapperConfigurationExpression> Configure { get; set; } = _ => { };
        public readonly List<Assembly> Assemblies = new() {typeof(AutoMapperModule).Assembly};
    }
}
