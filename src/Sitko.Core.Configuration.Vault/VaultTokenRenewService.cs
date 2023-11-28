using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VaultSharp;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.AuthMethods.Token;

namespace Sitko.Core.Configuration.Vault;

public class VaultTokenRenewService : BackgroundService
{
    private readonly ILogger<VaultTokenRenewService> logger;
    private readonly IOptionsMonitor<VaultConfigurationModuleOptions> optionsMonitor;

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
                    var vaultClientSettings = new VaultClientSettings(optionsMonitor.CurrentValue.Uri, optionsMonitor.CurrentValue.AuthType == VaultAuthType.Token
                        ? new TokenAuthMethodInfo(optionsMonitor.CurrentValue.Token)
                        : new AppRoleAuthMethodInfo(optionsMonitor.CurrentValue.VaultRoleId, optionsMonitor.CurrentValue.VaultSecret));
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
