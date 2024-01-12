using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Swagger;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddSwagger(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, SwaggerModuleOptions> configure, string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddSwagger(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddSwagger(this IHostApplicationBuilder hostApplicationBuilder,
        Action<SwaggerModuleOptions>? configure = null, string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddSwagger(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddSwagger(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, SwaggerModuleOptions> configure, string? optionsKey = null) =>
        applicationBuilder.AddModule<SwaggerModule, SwaggerModuleOptions>(configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddSwagger(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<SwaggerModuleOptions>? configure = null, string? optionsKey = null) =>
        applicationBuilder.AddModule<SwaggerModule, SwaggerModuleOptions>(configure, optionsKey);
}
