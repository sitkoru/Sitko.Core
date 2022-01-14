using System;
using System.Collections.Generic;
using System.Reflection;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Sitko.Core.App;

namespace Sitko.Core.Automapper;

public class AutoMapperModule : BaseApplicationModule<AutoMapperModuleOptions>
{
    public override string OptionsKey => "AutoMapper";

    public override void ConfigureServices(IApplicationContext context, IServiceCollection services,
        AutoMapperModuleOptions startupOptions)
    {
        base.ConfigureServices(context, services, startupOptions);
        services.AddAutoMapper(startupOptions.ConfigureMapper, startupOptions.Assemblies);
    }
}

public class AutoMapperModuleOptions : BaseModuleOptions
{
    [JsonIgnore] public Action<IMapperConfigurationExpression> ConfigureMapper { get; set; } = _ => { };
    [JsonIgnore] public List<Assembly> Assemblies { get; set; } = new() { typeof(AutoMapperModule).Assembly };
}
