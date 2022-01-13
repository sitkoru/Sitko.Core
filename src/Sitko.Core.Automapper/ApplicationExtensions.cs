using System;
using Microsoft.Extensions.Configuration;
using Sitko.Core.App;

namespace Sitko.Core.Automapper;

public static class ApplicationExtensions
{
    public static Application AddAutoMapper(this Application application,
        Action<IConfiguration, IAppEnvironment, AutoMapperModuleOptions> configure,
        string? optionsKey = null) =>
        application.AddModule<AutoMapperModule, AutoMapperModuleOptions>(configure, optionsKey);

    public static Application AddAutoMapper(this Application application,
        Action<AutoMapperModuleOptions>? configure = null,
        string? optionsKey = null) =>
        application.AddModule<AutoMapperModule, AutoMapperModuleOptions>(configure, optionsKey);
}
