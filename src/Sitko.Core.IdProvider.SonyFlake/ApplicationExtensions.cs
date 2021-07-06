using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.IdProvider.SonyFlake
{
    public static class ApplicationExtensions
    {
        public static Application AddSonyFlakeIdProvider(this Application application,
            Action<IConfiguration, IHostEnvironment, SonyFlakeIdProviderModuleOptions> configure,
            string? optionsKey = null)
        {
            return application.AddModule<SonyFlakeIdProviderModule, SonyFlakeIdProviderModuleOptions>(configure,
                optionsKey);
        }

        public static Application AddSonyFlakeIdProvider(this Application application,
            Action<SonyFlakeIdProviderModuleOptions>? configure = null,
            string? optionsKey = null)
        {
            return application.AddModule<SonyFlakeIdProviderModule, SonyFlakeIdProviderModuleOptions>(configure,
                optionsKey);
        }
    }
}
