using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Repository.EntityFrameworkCore
{
    using JetBrains.Annotations;

    [PublicAPI]
    public static class ApplicationExtensions
    {
        public static Application AddEFRepositories<TAssembly>(this Application application,
            Action<IConfiguration, IHostEnvironment, EFRepositoriesModuleOptions> configure,
            string? optionsKey = null) =>
            application.AddModule<EFRepositoriesModule<TAssembly>, EFRepositoriesModuleOptions>(configure,
                optionsKey);

        public static Application AddEFRepositories<TAssembly>(this Application application,
            Action<EFRepositoriesModuleOptions>? configure = null,
            string? optionsKey = null) =>
            application.AddModule<EFRepositoriesModule<TAssembly>, EFRepositoriesModuleOptions>(configure,
                optionsKey);
    }
}
