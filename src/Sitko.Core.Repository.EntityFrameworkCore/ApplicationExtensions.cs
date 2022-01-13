using System;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Sitko.Core.App;

namespace Sitko.Core.Repository.EntityFrameworkCore;

[PublicAPI]
public static class ApplicationExtensions
{
    public static Application AddEFRepositories(this Application application,
        Action<IConfiguration, IAppEnvironment, EFRepositoriesModuleOptions> configure,
        string? optionsKey = null) =>
        application.AddModule<EFRepositoriesModule, EFRepositoriesModuleOptions>(configure, optionsKey);

    public static Application AddEFRepositories(this Application application,
        Action<EFRepositoriesModuleOptions>? configure = null,
        string? optionsKey = null) =>
        application.AddModule<EFRepositoriesModule, EFRepositoriesModuleOptions>(configure, optionsKey);

    public static Application AddEFRepositories<TAssembly>(this Application application,
        Action<IConfiguration, IAppEnvironment, EFRepositoriesModuleOptions> configure,
        string? optionsKey = null) =>
        application.AddModule<EFRepositoriesModule, EFRepositoriesModuleOptions>(
            (configuration, environment, moduleOptions) =>
            {
                moduleOptions.AddRepositoriesFromAssemblyOf<TAssembly>();
                configure(configuration, environment, moduleOptions);
            },
            optionsKey);

    public static Application AddEFRepositories<TAssembly>(this Application application,
        Action<EFRepositoriesModuleOptions>? configure = null,
        string? optionsKey = null) =>
        application.AddModule<EFRepositoriesModule, EFRepositoriesModuleOptions>(moduleOptions =>
            {
                moduleOptions.AddRepositoriesFromAssemblyOf<TAssembly>();
                configure?.Invoke(moduleOptions);
            },
            optionsKey);
}
