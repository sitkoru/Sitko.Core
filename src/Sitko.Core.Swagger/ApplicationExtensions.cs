using System;
using Sitko.Core.App;

namespace Sitko.Core.Swagger;

public static class ApplicationExtensions
{
    public static Application AddSwagger(this Application application,
        Action<IApplicationContext, SwaggerModuleOptions> configure, string? optionsKey = null) =>
        application.AddModule<SwaggerModule, SwaggerModuleOptions>(configure, optionsKey);

    public static Application AddSwagger(this Application application,
        Action<SwaggerModuleOptions>? configure = null, string? optionsKey = null) =>
        application.AddModule<SwaggerModule, SwaggerModuleOptions>(configure, optionsKey);
}
