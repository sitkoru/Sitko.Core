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
        private static bool s_serviceRegistered;
        private static bool s_requireWatcher;

        public static Application AddVaultConfiguration(this Application application,
            Action<HostBuilderContext, VaultConfigurationOptions>? configureOptions = null)
        {
            application.ConfigureAppConfiguration((context, builder) =>
            {
                var options = new VaultConfigurationOptions();
                context.Configuration.Bind("vault", options);
                if (!options.Secrets.Any())
                {
                    options.Secrets.Add(application.Name);
                }

                configureOptions?.Invoke(context, options);

                if (string.IsNullOrEmpty(options.Uri))
                {
                    if (options.IsOptional)
                    {
                        return;
                    }

                    throw new Exception("Empty Vault uri");
                }

                if (string.IsNullOrEmpty(options.Token))
                {
                    if (options.IsOptional)
                    {
                        return;
                    }

                    throw new Exception("Empty Vault token");
                }

                if (string.IsNullOrEmpty(options.MountPoint))
                {
                    if (options.IsOptional)
                    {
                        return;
                    }

                    throw new Exception("Empty Vault mount point");
                }

                if (!options.Secrets.Any())
                {
                    if (options.IsOptional)
                    {
                        return;
                    }

                    throw new Exception("Empty Vault secrets list");
                }

                foreach (var secret in options.Secrets)
                {
                    builder.AddVaultConfiguration(options.GetOptions,
                        secret,
                        options.MountPoint);
                }

                if (options.ReloadOnChange)
                {
                    s_requireWatcher = true;
                }
            });
            application.ConfigureServices((context, services) =>
            {
                if (!s_requireWatcher || s_serviceRegistered)
                {
                    return;
                }

                services.TryAddSingleton((IConfigurationRoot)context.Configuration);
                services.AddHostedService<VaultChangeWatcher>();

                s_serviceRegistered = true;
            });
            return application;
        }
    }

    public class VaultConfigurationOptions
    {
        public List<string> Secrets { get; set; } = new();
        public string Uri { get; set; }
        public string Token { get; set; }
        public string MountPoint { get; set; } = "secret";
        public string? VaultSecret { get; set; } = null;
        public string? VaultRoleId { get; set; } = null;
        public bool ReloadOnChange { get; set; } = true;
        public int ReloadCheckIntervalSeconds { get; set; } = 60;
        public bool OmitVaultKeyName { get; set; } = false;

        public bool IsOptional { get; set; } = true;
        public IEnumerable<char>? AdditionalCharactersForConfigurationPath { get; set; } = null;

        public VaultOptions GetOptions()
        {
            return new(Uri, Token, VaultSecret, VaultRoleId, ReloadOnChange, ReloadCheckIntervalSeconds,
                OmitVaultKeyName, AdditionalCharactersForConfigurationPath);
        }
    }
}
