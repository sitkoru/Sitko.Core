using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using VaultSharp.Extensions.Configuration;

namespace Sitko.Core.Configuration.Vault
{
    public static class ApplicationExtensions
    {
        private static bool _serviceRegistered;
        private static bool _hasSecrets;
        private static bool _isEnabled;

        public static Application AddVaultConfiguration(this Application application,
            Action<HostBuilderContext, VaultConfigurationOptions>? configureOptions = null,
            Func<HostBuilderContext, bool>? isEnabled = null)
        {
            application.ConfigureAppConfiguration((context, builder) =>
            {
                if (isEnabled is not null && !isEnabled(context))
                {
                    return;
                }

                _isEnabled = true;

                var options = new VaultConfigurationOptions();
                context.Configuration.Bind("vault", options);
                configureOptions?.Invoke(context, options);
                if (!options.Secrets.Any())
                {
                    return;
                }

                _hasSecrets = true;
                foreach (var secret in options.Secrets)
                {
                    builder.AddVaultConfiguration(options.GetOptions,
                        secret,
                        options.MountPoint);
                }
            });
            application.ConfigureServices((context, services) =>
            {
                if (!_isEnabled || !_hasSecrets || _serviceRegistered)
                {
                    return;
                }

                services.TryAddSingleton((IConfigurationRoot)context.Configuration);
                services.AddHostedService<VaultChangeWatcher>();

                _serviceRegistered = true;
            });
            return application;
        }
    }

    public class VaultConfigurationOptions
    {
        public List<string> Secrets { get; set; } = new();
        public string Uri { get; set; }
        public string Token { get; set; }
        public string MountPoint { get; set; }
        public string? VaultSecret { get; set; } = null;
        public string? VaultRoleId { get; set; } = null;
        public bool ReloadOnChange { get; set; } = true;
        public int ReloadCheckIntervalSeconds { get; set; } = 60;
        public bool OmitVaultKeyName { get; set; } = false;
        public IEnumerable<char>? AdditionalCharactersForConfigurationPath { get; set; } = null;

        public VaultOptions GetOptions()
        {
            return new(Uri, Token, VaultSecret, VaultRoleId, ReloadOnChange, ReloadCheckIntervalSeconds,
                OmitVaultKeyName, AdditionalCharactersForConfigurationPath);
        }
    }
}
