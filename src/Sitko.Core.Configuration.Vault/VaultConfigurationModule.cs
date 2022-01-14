using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.App;
using VaultSharp.Extensions.Configuration;

namespace Sitko.Core.Configuration.Vault;

public class VaultConfigurationModule : BaseApplicationModule<VaultConfigurationModuleOptions>,
    IConfigurationModule<VaultConfigurationModuleOptions>
{
    public override string OptionsKey => "Vault";

    public void ConfigureAppConfiguration(IConfigurationBuilder configurationBuilder,
        VaultConfigurationModuleOptions startupOptions)
    {
        foreach (var secret in startupOptions.Secrets)
        {
            configurationBuilder.AddVaultConfiguration(startupOptions.GetOptions,
                secret,
                startupOptions.MountPoint);
        }
    }

    public void CheckConfiguration(IApplicationContext context, IServiceProvider serviceProvider)
    {
        var root = serviceProvider.GetRequiredService<IConfigurationRoot>();
        var providers = root.Providers.OfType<VaultConfigurationProvider>().ToList();
        if (!providers.Any())
        {
            throw new InvalidOperationException("No Vault providers on configuration");
        }

        var property = typeof(VaultConfigurationProvider).GetProperty("ConfigurationSource",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var emptySecrets = new List<string>();
        foreach (var provider in providers)
        {
            if (property!.GetValue(provider) is VaultConfigurationSource source)
            {
                var keys = provider.GetChildKeys(Array.Empty<string>(), null).ToArray();
                serviceProvider.GetRequiredService<ILogger<VaultConfigurationModule>>()
                    .LogInformation("Loaded {KeysCount} keys from secret {Secret}", keys.Length, source.BasePath);
                if (!keys.Any())
                {
                    emptySecrets.Add(source.BasePath);
                }
            }
        }

        var options = serviceProvider.GetRequiredService<IOptions<VaultConfigurationModuleOptions>>();
        if (emptySecrets.Any() && options.Value.ThrowOnEmptySecrets)
        {
            var names = string.Join(", ", emptySecrets);
            throw new OptionsValidationException(names, GetType(),
                new[] { $"No data loaded from Vault secrets {names}" });
        }
    }

    public override void ConfigureServices(IApplicationContext context, IServiceCollection services,
        VaultConfigurationModuleOptions startupOptions)
    {
        base.ConfigureServices(context, services, startupOptions);
        if (startupOptions.ReloadOnChange)
        {
            services.TryAddSingleton((IConfigurationRoot)context.Configuration);
            services.AddHostedService<VaultChangeWatcher>();
        }

        if (startupOptions.RenewToken)
        {
            services.AddHostedService<VaultTokenRenewService>();
        }
    }
}

public class VaultConfigurationModuleOptions : BaseModuleOptions
{
    public List<string> Secrets { get; set; } = new();
    public string Uri { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string MountPoint { get; set; } = "secret";
    public string? VaultSecret { get; set; } = null;
    public string? VaultRoleId { get; set; } = null;
    public bool ReloadOnChange { get; set; } = true;
    public int ReloadCheckIntervalSeconds { get; set; } = 60;
    public bool OmitVaultKeyName { get; set; } = false;
    public bool RenewToken { get; set; } = true;
    public int TokenRenewIntervalMinutes { get; set; } = 60;
    public bool ThrowOnEmptySecrets { get; set; } = true;

    public IEnumerable<char>? AdditionalCharactersForConfigurationPath { get; set; } = null;

    public VaultOptions GetOptions() =>
        new(Uri, Token, VaultSecret, VaultRoleId, ReloadOnChange, ReloadCheckIntervalSeconds,
            OmitVaultKeyName, AdditionalCharactersForConfigurationPath);

    public override void Configure(IApplicationContext applicationContext)
    {
        if (!Secrets.Any())
        {
            Secrets.Add(applicationContext.Name);
        }
    }
}

public class VaultConfigurationOptionsValidator : AbstractValidator<VaultConfigurationModuleOptions>
{
    public VaultConfigurationOptionsValidator()
    {
        RuleFor(o => o.Uri).NotEmpty().WithMessage("Vault url is empty");
        RuleFor(o => o.Token).NotEmpty().WithMessage("Vault token is empty");
        RuleFor(o => o.MountPoint).NotEmpty().WithMessage("Vault mount point is empty");
        RuleFor(o => o.Secrets).NotEmpty().WithMessage("Vault secrets list is empty");
    }
}
