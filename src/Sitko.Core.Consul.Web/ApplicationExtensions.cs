﻿using Sitko.Core.App;

namespace Sitko.Core.Consul.Web;

public static class ApplicationExtensions
{
    public static Application AddConsulWeb(this Application application,
        Action<IApplicationContext, ConsulWebModuleOptions> configure,
        string? optionsKey = null) =>
        application.AddModule<ConsulWebModule, ConsulWebModuleOptions>(configure, optionsKey);

    public static Application AddConsulWeb(this Application application,
        Action<ConsulWebModuleOptions>? configure = null,
        string? optionsKey = null) =>
        application.AddModule<ConsulWebModule, ConsulWebModuleOptions>(configure, optionsKey);
}

