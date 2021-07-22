using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Auth.Google
{
    public static class ApplicationExtensions
    {
        public static Application AddGoogleAuth(this Application application,
            Action<IConfiguration, IHostEnvironment, GoogleAuthModuleOptions> configure, string? optionsKey = null) =>
            application.AddModule<GoogleAuthModule, GoogleAuthModuleOptions>(configure, optionsKey);

        public static Application AddGoogleAuth(this Application application,
            Action<GoogleAuthModuleOptions>? configure = null, string? optionsKey = null) =>
            application.AddModule<GoogleAuthModule, GoogleAuthModuleOptions>(configure, optionsKey);
    }
}
