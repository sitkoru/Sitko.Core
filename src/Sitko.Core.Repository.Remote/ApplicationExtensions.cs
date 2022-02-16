using JetBrains.Annotations;
using Sitko.Core.App;

namespace Sitko.Core.Repository.Remote;

[PublicAPI]
public static class ApplicationExtensions
{
    public static Application AddRemoteRepositories(this Application application,
        Action<IApplicationContext, RemoteRepositoryOptions> configure,
        string? optionsKey = null) =>
        application.AddModule<RemoteRepositoryModule, RemoteRepositoryOptions>(configure, optionsKey);

    public static Application AddRemoteRepositories(this Application application,
        Action<RemoteRepositoryOptions>? configure = null,
        string? optionsKey = null) =>
        application.AddModule<RemoteRepositoryModule, RemoteRepositoryOptions>(configure, optionsKey);

    public static Application AddRemoteRepositories<TAssembly>(this Application application,
        Action<IApplicationContext, RemoteRepositoryOptions> configure,
        string? optionsKey = null) =>
        application.AddModule<RemoteRepositoryModule, RemoteRepositoryOptions>(
            (applicationContext, moduleOptions) =>
            {
                moduleOptions.AddRepositoriesFromAssemblyOf<TAssembly>();
                configure(applicationContext, moduleOptions);
            },
            optionsKey);

    public static Application AddRemoteRepositories<TAssembly>(this Application application,
        Action<RemoteRepositoryOptions>? configure = null,
        string? optionsKey = null) =>
        application.AddModule<RemoteRepositoryModule, RemoteRepositoryOptions>(moduleOptions =>
            {
                moduleOptions.AddRepositoriesFromAssemblyOf<TAssembly>();
                configure?.Invoke(moduleOptions);
            },
            optionsKey);

    public static Application AddHttpRepositoryTransport(this Application application,
        Action<HttpRepositoryTransportOptions>? configure = null,
        string? optionsKey = null) =>
        application.AddModule<HttpRepositoryTransportModule, HttpRepositoryTransportOptions>(configure,optionsKey);
}
