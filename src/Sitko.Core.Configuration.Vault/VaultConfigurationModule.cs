using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using VaultSharp.Extensions.Configuration;

namespace Sitko.Core.Configuration.Vault
{
    public class VaultConfigurationModule : BaseApplicationModule<VaultConfigurationModuleOptions>
    {
        public override string GetOptionsKey()
        {
            return "Vault";
        }

        public override void ConfigureAppConfiguration(ApplicationContext context,
            HostBuilderContext hostBuilderContext,
            IConfigurationBuilder configurationBuilder, VaultConfigurationModuleOptions startupOptions)
        {
            base.ConfigureAppConfiguration(context, hostBuilderContext, configurationBuilder, startupOptions);

            foreach (var secret in startupOptions.Secrets)
            {
                configurationBuilder.AddVaultConfiguration(startupOptions.GetOptions,
                    secret,
                    startupOptions.MountPoint);
            }
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
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

        public bool IsOptional { get; set; } = true;

        public bool RenewToken { get; set; } = true;
        public int TokenRenewIntervalMinutes { get; set; } = 60;

        public IEnumerable<char>? AdditionalCharactersForConfigurationPath { get; set; } = null;

        public VaultOptions GetOptions()
        {
            return new(Uri, Token, VaultSecret, VaultRoleId, ReloadOnChange, ReloadCheckIntervalSeconds,
                OmitVaultKeyName, AdditionalCharactersForConfigurationPath);
        }

        public override void Configure(ApplicationContext applicationContext)
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
}
