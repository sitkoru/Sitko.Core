using System;
using Microsoft.Extensions.Configuration;
using Sitko.Core.App;

namespace Sitko.Core.Configuration.Vault;

public static class ApplicationExtensions
{
    public static Application AddVaultConfiguration(this Application application,
        Action<IConfiguration, IAppEnvironment, VaultConfigurationModuleOptions> configure,
        string? optionsKey = null) =>
        application.AddModule<VaultConfigurationModule, VaultConfigurationModuleOptions>(configure,
            optionsKey);

    public static Application AddVaultConfiguration(this Application application,
        Action<VaultConfigurationModuleOptions>? configure = null, string? optionsKey = null) =>
        application.AddModule<VaultConfigurationModule, VaultConfigurationModuleOptions>(configure,
            optionsKey);
}
