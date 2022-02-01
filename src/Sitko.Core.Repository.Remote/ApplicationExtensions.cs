using JetBrains.Annotations;
using Sitko.Core.App;

namespace Sitko.Core.Repository.Remote;

[PublicAPI]
public static class ApplicationExtensions
{

    public static Application AddRemoteRepositories(this Application application,
        Action<IApplicationContext, RemoteRepositoryModuleOptions> configure,
        string? optionsKey = null) =>
        application.AddModule<RemoteRepositoryModule, RemoteRepositoryModuleOptions>(configure, optionsKey);

    public static Application AddRemoteRepositories(this Application application,
        Action<RemoteRepositoryModuleOptions>? configure = null,
        string? optionsKey = null) =>
        application.AddModule<RemoteRepositoryModule, RemoteRepositoryModuleOptions>(configure, optionsKey);

    public static Application AddRemoteRepositories<TAssembly>(this Application application,
        Action<IApplicationContext, RemoteRepositoryModuleOptions> configure,
        string? optionsKey = null) =>
        application.AddModule<RemoteRepositoryModule, RemoteRepositoryModuleOptions>(
            (applicationContext, moduleOptions) =>
            {
                moduleOptions.AddRepositoriesFromAssemblyOf<TAssembly>();
                configure(applicationContext, moduleOptions);
            },
            optionsKey);

    public static Application AddRemoteRepositories<TAssembly>(this Application application,
        Action<RemoteRepositoryModuleOptions>? configure = null,
        string? optionsKey = null) =>
        application.AddModule<RemoteRepositoryModule, RemoteRepositoryModuleOptions>(moduleOptions =>
            {
                moduleOptions.AddRepositoriesFromAssemblyOf<TAssembly>();
                configure?.Invoke(moduleOptions);
            },
            optionsKey);
}
