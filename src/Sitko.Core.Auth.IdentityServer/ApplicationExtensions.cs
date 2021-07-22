using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Auth.IdentityServer
{
    public static class ApplicationExtensions
    {
        public static Application AddJwtIdentityServer(this Application application,
            Action<IConfiguration, IHostEnvironment, JwtIdentityServerModuleOptions> configure,
            string? optionsKey = null) =>
            application
                .AddModule<JwtIdentityServerModule, JwtIdentityServerModuleOptions>(configure, optionsKey);

        public static Application AddJwtIdentityServer(this Application application,
            Action<JwtIdentityServerModuleOptions>? configure = null, string? optionsKey = null) =>
            application
                .AddModule<JwtIdentityServerModule, JwtIdentityServerModuleOptions>(configure, optionsKey);

        public static Application AddOidcIdentityServer(this Application application,
            Action<IConfiguration, IHostEnvironment, OidcIdentityServerModuleOptions> configure,
            string? optionsKey = null) =>
            application.AddModule<OidcIdentityServerModule, OidcIdentityServerModuleOptions>(configure,
                optionsKey);

        public static Application AddOidcIdentityServer(this Application application,
            Action<OidcIdentityServerModuleOptions>? configure = null, string? optionsKey = null) =>
            application.AddModule<OidcIdentityServerModule, OidcIdentityServerModuleOptions>(configure,
                optionsKey);
    }
}
