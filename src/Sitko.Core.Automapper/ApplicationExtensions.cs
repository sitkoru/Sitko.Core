using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Automapper
{
    public static class ApplicationExtensions
    {
        public static Application AddAutoMapper(this Application application,
            Action<IConfiguration, IHostEnvironment, AutoMapperModuleOptions> configure,
            string? optionsKey = null)
        {
            return application.AddModule<AutoMapperModule, AutoMapperModuleOptions>(configure, optionsKey);
        }

        public static Application AddAutoMapper(this Application application,
            Action<AutoMapperModuleOptions>? configure = null,
            string? optionsKey = null)
        {
            return application.AddModule<AutoMapperModule, AutoMapperModuleOptions>(configure, optionsKey);
        }
    }
}
