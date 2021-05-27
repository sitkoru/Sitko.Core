using System;
using System.Collections.Generic;
using System.Reflection;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Automapper
{
    public class AutoMapperModule : BaseApplicationModule<AutoMapperModuleOptions>
    {
        public override string GetOptionsKey()
        {
            return "AutoMapper";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            AutoMapperModuleOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddAutoMapper(startupOptions.Configure, startupOptions.Assemblies);
        }
    }

    public class AutoMapperModuleOptions : BaseModuleOptions
    {
        public Action<IMapperConfigurationExpression> Configure { get; set; } = _ => { };
        public readonly List<Assembly> Assemblies = new() {typeof(AutoMapperModule).Assembly};
    }
}
