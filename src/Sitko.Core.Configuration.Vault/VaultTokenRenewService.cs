using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace Sitko.Core.Configuration.Vault
{
    public class VaultTokenRenewService : BackgroundService
    {
        private readonly IOptionsMonitor<VaultConfigurationModuleOptions> optionsMonitor;
        private readonly ILogger<VaultTokenRenewService> logger;

        public VaultTokenRenewService(IOptionsMonitor<VaultConfigurationModuleOptions> optionsMonitor,
            ILogger<VaultTokenRenewService> logger)
        {
            this.optionsMonitor = optionsMonitor;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Start vault token renew service");
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(optionsMonitor.CurrentValue.TokenRenewIntervalMinutes),
                        stoppingToken)
                    .ConfigureAwait(false);
                if (optionsMonitor.CurrentValue.RenewToken)
                {
                    logger.LogInformation("Renew vault token");
                    try
                    {
                        var authMethod = new TokenAuthMethodInfo(optionsMonitor.CurrentValue.Token);
                        var vaultClientSettings = new VaultClientSettings(optionsMonitor.CurrentValue.Uri, authMethod);
                        var vaultClient = new VaultClient(vaultClientSettings);

                        var result = await vaultClient.V1.Auth.Token.RenewSelfAsync().ConfigureAwait(false);
                        logger.LogInformation("Token renewed. {IsRenewable}. {LeaseDuration}", result.Renewable,
                            result.LeaseDurationSeconds);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error renewing vault token: {ErrorText}", ex.ToString());
                    }
                }
                else
                {
                    logger.LogInformation("Vault token renew disabled");
                }
            }

            logger.LogInformation("Stop vault token renew service");
        }
    }
}
