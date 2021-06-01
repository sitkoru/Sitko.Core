using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
            application.ConfigureAppConfiguration((applicationContext, hostBuilderContext, configurationBuilder) =>
            {
                var options = new VaultConfigurationOptions();
                hostBuilderContext.Configuration.Bind("vault", options);
                if (!options.Secrets.Any())
                {
                    options.Secrets.Add(application.Name);
                }

                configureOptions?.Invoke(hostBuilderContext, options);

                var isOptional = options.IsOptional || application.IsPostBuildCheckRun;
                var validator = new VaultConfigurationOptionsValidator();
                var result = validator.Validate(options);
                if (!result.IsValid)
                {
                    foreach (var error in result.Errors)
                    {
                        applicationContext.Logger.LogError("Vault config error: {ErrorMessage}", error.ErrorMessage);
                    }

                    if (!isOptional)
                    {
                        throw new Exception("Vault configuration failed");
                    }
                }
                else
                {
                    foreach (var secret in options.Secrets)
                    {
                        configurationBuilder.AddVaultConfiguration(options.GetOptions,
                            secret,
                            options.MountPoint);
                    }

                    if (options.ReloadOnChange)
                    {
                        s_requireWatcher = true;
                    }
                }
            });
            application.ConfigureServices((_, context, services) =>
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

    public class VaultConfigurationOptionsValidator : AbstractValidator<VaultConfigurationOptions>
    {
        public VaultConfigurationOptionsValidator()
        {
            RuleFor(o => o.Uri).NotEmpty().WithMessage("Vault url is empty");
            RuleFor(o => o.Token).NotEmpty().WithMessage("Vault token is empty");
            RuleFor(o => o.MountPoint).NotEmpty().WithMessage("Vault mount point is empty");
            RuleFor(o => o.Secrets).NotEmpty().WithMessage("Vault secrets list is empty");
        }
    }
}
