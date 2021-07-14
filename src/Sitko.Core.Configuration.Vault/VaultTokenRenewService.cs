using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace Sitko.Core.Configuration.Vault
{
    public class VaultTokenRenewService : BackgroundService
    {
        private readonly IOptionsMonitor<VaultConfigurationModuleOptions> _optionsMonitor;

        public VaultTokenRenewService(IOptionsMonitor<VaultConfigurationModuleOptions> optionsMonitor)
        {
            _optionsMonitor = optionsMonitor;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.FromResult(TimeSpan.FromMinutes(_optionsMonitor.CurrentValue.TokenRenewIntervalMinutes))
                    .ConfigureAwait(false);
                if (_optionsMonitor.CurrentValue.RenewToken)
                {
                    var authMethod = new TokenAuthMethodInfo(_optionsMonitor.CurrentValue.Token);
                    var vaultClientSettings = new VaultClientSettings(_optionsMonitor.CurrentValue.Uri, authMethod);
                    var vaultClient = new VaultClient(vaultClientSettings);
                    await vaultClient.V1.Auth.Token.RenewSelfAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
