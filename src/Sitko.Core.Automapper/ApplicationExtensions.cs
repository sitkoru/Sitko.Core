using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Automapper;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddAutoMapper(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, AutoMapperModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddAutoMapper(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddAutoMapper(this IHostApplicationBuilder hostApplicationBuilder,
        Action<AutoMapperModuleOptions>? configure = null,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddAutoMapper(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static SitkoCoreApplicationBuilder AddAutoMapper(this SitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, AutoMapperModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<AutoMapperModule, AutoMapperModuleOptions>(configure, optionsKey);

    public static SitkoCoreApplicationBuilder AddAutoMapper(this SitkoCoreApplicationBuilder applicationBuilder,
        Action<AutoMapperModuleOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<AutoMapperModule, AutoMapperModuleOptions>(configure, optionsKey);
}
