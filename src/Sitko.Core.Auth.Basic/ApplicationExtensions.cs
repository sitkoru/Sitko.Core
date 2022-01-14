using System;
using Sitko.Core.App;

namespace Sitko.Core.Auth.Basic;

public static class ApplicationExtensions
{
    public static Application AddBasicAuth(this Application application,
        Action<IApplicationContext, BasicAuthModuleOptions> configure, string? optionsKey = null) =>
        application.AddModule<BasicAuthModule, BasicAuthModuleOptions>(configure, optionsKey);

    public static Application AddBasicAuth(this Application application,
        Action<BasicAuthModuleOptions>? configure = null, string? optionsKey = null) =>
        application.AddModule<BasicAuthModule, BasicAuthModuleOptions>(configure, optionsKey);
}
