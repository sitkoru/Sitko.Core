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

    public static ISitkoCoreApplicationBuilder AddRemoteRepositories(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, RemoteRepositoryOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<RemoteRepositoryModule, RemoteRepositoryOptions>(configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddRemoteRepositories(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<RemoteRepositoryOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<RemoteRepositoryModule, RemoteRepositoryOptions>(configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddRemoteRepositories<TAssembly>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, RemoteRepositoryOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<RemoteRepositoryModule, RemoteRepositoryOptions>(
            (applicationContext, moduleOptions) =>
            {
                moduleOptions.AddRepositoriesFromAssemblyOf<TAssembly>();
                configure(applicationContext, moduleOptions);
            },
            optionsKey);

    public static ISitkoCoreApplicationBuilder AddRemoteRepositories<TAssembly>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<RemoteRepositoryOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<RemoteRepositoryModule, RemoteRepositoryOptions>(moduleOptions =>
            {
                moduleOptions.AddRepositoriesFromAssemblyOf<TAssembly>();
                configure?.Invoke(moduleOptions);
            },
            optionsKey);

    public static ISitkoCoreApplicationBuilder AddHttpRepositoryTransport(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<HttpRepositoryTransportOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<HttpRepositoryTransportModule, HttpRepositoryTransportOptions>(configure,
            optionsKey);

    public static ISitkoCoreApplicationBuilder AddHttpRepositoryTransport(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, HttpRepositoryTransportOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<HttpRepositoryTransportModule, HttpRepositoryTransportOptions>(configure,
            optionsKey);
}
