using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sitko.Core.App;
using Sitko.Core.App.Health;
using Sitko.Core.Auth.OIDC.Tokens;

namespace Sitko.Core.Auth.OIDC;

internal static class OidcProviderModuleChecks
{
    internal static readonly ConcurrentDictionary<string, bool> Checks = new();
}

public abstract class OidcProviderModule<TAuthOptions> : AuthModule<TAuthOptions>
    where TAuthOptions : OidcProviderAuthOptions, new()
{
    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TAuthOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.TryAddScoped<IUserTokenProvider, UserTokenProvider>();
        if (startupOptions.EnableHealthChecks)
        {
            if (Uri.TryCreate(startupOptions.OidcServerUrl, UriKind.Absolute, out var oidcUri))
            {
                if (OidcProviderModuleChecks.Checks.TryAdd(oidcUri.ToString(), true))
                {
                    services.AddHealthChecks().AddOpenIdConnectServer(oidcUri, name: $"IdSrv: {oidcUri}",
                        failureStatus: HealthStatus.Unhealthy,
                        tags: HealthCheckStages.GetSkipTags(HealthCheckStages.Liveness, HealthCheckStages.Readiness));
                }
            }
        }
    }
}
