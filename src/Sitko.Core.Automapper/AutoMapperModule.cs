using System;
using System.Collections.Generic;
using System.Reflection;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Automapper
{
    using Newtonsoft.Json;

    public class AutoMapperModule : BaseApplicationModule<AutoMapperModuleOptions>
    {
        public override string OptionsKey => "AutoMapper";

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            AutoMapperModuleOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddAutoMapper(startupOptions.Configure, startupOptions.Assemblies);
        }
    }

    public class AutoMapperModuleOptions : BaseModuleOptions
    {
        [JsonIgnore]
        public Action<IMapperConfigurationExpression> Configure { get; set; } = _ => { };
        [JsonIgnore]
        public readonly List<Assembly> Assemblies = new() {typeof(AutoMapperModule).Assembly};
    }
}
