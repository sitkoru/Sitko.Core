using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Repository.EntityFrameworkCore
{
    public static class ApplicationExtensions
    {
        public static Application AddEFRepositories<TAssembly>(this Application application,
            Action<IConfiguration, IHostEnvironment, EFRepositoriesModuleOptions> configure,
            string? optionsKey = null)
        {
            return application.AddModule<EFRepositoriesModule<TAssembly>, EFRepositoriesModuleOptions>(configure,
                optionsKey);
        }

        public static Application AddEFRepositories<TAssembly>(this Application application,
            Action<EFRepositoriesModuleOptions>? configure = null,
            string? optionsKey = null)
        {
            return application.AddModule<EFRepositoriesModule<TAssembly>, EFRepositoriesModuleOptions>(configure,
                optionsKey);
        }
    }
}
