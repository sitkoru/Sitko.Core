using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using VaultSharp.Extensions.Configuration;

namespace Sitko.Core.Configuration.Vault
{
    public static class ApplicationExtensions
    {
        public static Application AddVaultConfiguration(this Application application,
            Func<VaultOptions> configureOptions, string path, string mountPoint)
        {
            application.ConfigureAppConfiguration((context, builder) =>
            {
                builder.AddVaultConfiguration(configureOptions,
                    path,
                    mountPoint);
            });
            application.ConfigureServices((context, services) =>
            {
                services.AddSingleton((IConfigurationRoot)context.Configuration);
                services.AddHostedService<VaultChangeWatcher>();
            });
            return application;
        }
    }
}
