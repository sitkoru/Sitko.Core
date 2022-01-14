using System;
using Sitko.Core.App;

namespace Sitko.Core.IdProvider.SonyFlake;

public static class ApplicationExtensions
{
    public static Application AddSonyFlakeIdProvider(this Application application,
        Action<IApplicationContext, SonyFlakeIdProviderModuleOptions> configure,
        string? optionsKey = null) =>
        application.AddModule<SonyFlakeIdProviderModule, SonyFlakeIdProviderModuleOptions>(configure,
            optionsKey);

    public static Application AddSonyFlakeIdProvider(this Application application,
        Action<SonyFlakeIdProviderModuleOptions>? configure = null,
        string? optionsKey = null) =>
        application.AddModule<SonyFlakeIdProviderModule, SonyFlakeIdProviderModuleOptions>(configure,
            optionsKey);
}
