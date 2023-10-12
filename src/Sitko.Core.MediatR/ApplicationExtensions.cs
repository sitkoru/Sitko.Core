using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.MediatR;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddMediatR<TAssembly>(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, MediatRModuleOptions<TAssembly>> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddMediatR(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddMediatR<TAssembly>(this IHostApplicationBuilder hostApplicationBuilder,
        Action<MediatRModuleOptions<TAssembly>>? configure = null,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddMediatR(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static SitkoCoreApplicationBuilder AddMediatR<TAssembly>(this SitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, MediatRModuleOptions<TAssembly>> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<MediatRModule<TAssembly>, MediatRModuleOptions<TAssembly>>(configure,
            optionsKey);

    public static SitkoCoreApplicationBuilder AddMediatR<TAssembly>(this SitkoCoreApplicationBuilder applicationBuilder,
        Action<MediatRModuleOptions<TAssembly>>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<MediatRModule<TAssembly>, MediatRModuleOptions<TAssembly>>(configure,
            optionsKey);
}
