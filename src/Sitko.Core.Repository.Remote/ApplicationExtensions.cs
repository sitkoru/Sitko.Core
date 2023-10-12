using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Repository.Remote;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddRemoteRepositories(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, RemoteRepositoryOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddRemoteRepositories(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddRemoteRepositories(this IHostApplicationBuilder hostApplicationBuilder,
        Action<RemoteRepositoryOptions>? configure = null,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddRemoteRepositories(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddRemoteRepositories<TAssembly>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, RemoteRepositoryOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddRemoteRepositories<TAssembly>(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddRemoteRepositories<TAssembly>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<RemoteRepositoryOptions>? configure = null,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddRemoteRepositories<TAssembly>(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddHttpRepositoryTransport(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<HttpRepositoryTransportOptions>? configure = null,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddHttpRepositoryTransport(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddHttpRepositoryTransport(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, HttpRepositoryTransportOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddHttpRepositoryTransport(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static SitkoCoreApplicationBuilder AddRemoteRepositories(this SitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, RemoteRepositoryOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<RemoteRepositoryModule, RemoteRepositoryOptions>(configure, optionsKey);

    public static SitkoCoreApplicationBuilder AddRemoteRepositories(this SitkoCoreApplicationBuilder applicationBuilder,
        Action<RemoteRepositoryOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<RemoteRepositoryModule, RemoteRepositoryOptions>(configure, optionsKey);

    public static SitkoCoreApplicationBuilder AddRemoteRepositories<TAssembly>(
        this SitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, RemoteRepositoryOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<RemoteRepositoryModule, RemoteRepositoryOptions>(
            (applicationContext, moduleOptions) =>
            {
                moduleOptions.AddRepositoriesFromAssemblyOf<TAssembly>();
                configure(applicationContext, moduleOptions);
            },
            optionsKey);

    public static SitkoCoreApplicationBuilder AddRemoteRepositories<TAssembly>(
        this SitkoCoreApplicationBuilder applicationBuilder,
        Action<RemoteRepositoryOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<RemoteRepositoryModule, RemoteRepositoryOptions>(moduleOptions =>
            {
                moduleOptions.AddRepositoriesFromAssemblyOf<TAssembly>();
                configure?.Invoke(moduleOptions);
            },
            optionsKey);

    public static SitkoCoreApplicationBuilder AddHttpRepositoryTransport(
        this SitkoCoreApplicationBuilder applicationBuilder,
        Action<HttpRepositoryTransportOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<HttpRepositoryTransportModule, HttpRepositoryTransportOptions>(configure,
            optionsKey);

    public static SitkoCoreApplicationBuilder AddHttpRepositoryTransport(
        this SitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, HttpRepositoryTransportOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<HttpRepositoryTransportModule, HttpRepositoryTransportOptions>(configure,
            optionsKey);
}
