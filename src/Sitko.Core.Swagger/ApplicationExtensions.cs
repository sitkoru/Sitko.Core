using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Swagger
{
    public static class ApplicationExtensions
    {
        public static Application AddSwagger(this Application application,
            Action<IConfiguration, IHostEnvironment, SwaggerModuleOptions> configure, string? optionsKey = null)
        {
            return application.AddModule<SwaggerModule, SwaggerModuleOptions>(configure, optionsKey);
        }

        public static Application AddSwagger(this Application application,
            Action<SwaggerModuleOptions>? configure = null, string? optionsKey = null)
        {
            return application.AddModule<SwaggerModule, SwaggerModuleOptions>(configure, optionsKey);
        }
    }
}
