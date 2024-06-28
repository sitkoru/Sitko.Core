using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.ServiceDiscovery;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddServiceDiscovery<TRegistrar, TResolver>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, ServiceDiscoveryModuleOptions> configure,
        string? optionsKey = null)
        where TRegistrar : class, IServiceDiscoveryRegistrar
        where TResolver : class, IServiceDiscoveryResolver
    {
        hostApplicationBuilder.GetSitkoCore().AddServiceDiscovery<TRegistrar, TResolver>(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddServiceDiscovery<TRegistrar, TResolver>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<ServiceDiscoveryModuleOptions>? configure = null,
        string? optionsKey = null)
        where TRegistrar : class, IServiceDiscoveryRegistrar
        where TResolver : class, IServiceDiscoveryResolver
    {
        hostApplicationBuilder.GetSitkoCore().AddServiceDiscovery<TRegistrar, TResolver>(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddServiceDiscovery<TRegistrar, TResolver>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, ServiceDiscoveryModuleOptions> configure,
        string? optionsKey = null) where TRegistrar : class, IServiceDiscoveryRegistrar
        where TResolver : class, IServiceDiscoveryResolver =>
        applicationBuilder.AddModule<ServiceDiscoveryModule<TRegistrar, TResolver>, ServiceDiscoveryModuleOptions>(
            configure,
            optionsKey);

    public static ISitkoCoreApplicationBuilder AddServiceDiscovery<TRegistrar, TResolver>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<ServiceDiscoveryModuleOptions>? configure = null,
        string? optionsKey = null)
        where TResolver : class, IServiceDiscoveryResolver where TRegistrar : class, IServiceDiscoveryRegistrar =>
        applicationBuilder.AddModule<ServiceDiscoveryModule<TRegistrar, TResolver>, ServiceDiscoveryModuleOptions>(
            configure,
            optionsKey);

    public static IHostApplicationBuilder AddServiceDiscovery<TResolver>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, ServiceDiscoveryModuleOptions> configure,
        string? optionsKey = null)
        where TResolver : class, IServiceDiscoveryResolver
    {
        hostApplicationBuilder.GetSitkoCore().AddServiceDiscovery<TResolver>(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddServiceDiscovery<TResolver>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<ServiceDiscoveryModuleOptions>? configure = null,
        string? optionsKey = null)
        where TResolver : class, IServiceDiscoveryResolver
    {
        hostApplicationBuilder.GetSitkoCore().AddServiceDiscovery<TResolver>(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddServiceDiscovery<TResolver>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, ServiceDiscoveryModuleOptions> configure,
        string? optionsKey = null)
        where TResolver : class, IServiceDiscoveryResolver =>
        applicationBuilder
            .AddModule<ServiceDiscoveryModule<NopeServiceDiscoveryRegistrar, TResolver>, ServiceDiscoveryModuleOptions>(
                configure,
                optionsKey);

    public static ISitkoCoreApplicationBuilder AddServiceDiscovery<TResolver>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<ServiceDiscoveryModuleOptions>? configure = null,
        string? optionsKey = null)
        where TResolver : class, IServiceDiscoveryResolver =>
        applicationBuilder
            .AddModule<ServiceDiscoveryModule<NopeServiceDiscoveryRegistrar, TResolver>, ServiceDiscoveryModuleOptions>(
                configure,
                optionsKey);
}
