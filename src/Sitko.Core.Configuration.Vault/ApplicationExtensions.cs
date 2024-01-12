using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Configuration.Vault;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddVaultConfiguration(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, VaultConfigurationModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddVaultConfiguration(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddVaultConfiguration(this IHostApplicationBuilder hostApplicationBuilder,
        Action<VaultConfigurationModuleOptions>? configure = null, string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddVaultConfiguration(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddVaultConfiguration(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, VaultConfigurationModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<VaultConfigurationModule, VaultConfigurationModuleOptions>(configure,
            optionsKey);

    public static ISitkoCoreApplicationBuilder AddVaultConfiguration(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<VaultConfigurationModuleOptions>? configure = null, string? optionsKey = null) =>
        applicationBuilder.AddModule<VaultConfigurationModule, VaultConfigurationModuleOptions>(configure,
            optionsKey);
}
