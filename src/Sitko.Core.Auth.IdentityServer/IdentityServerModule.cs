using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Auth.IdentityServer;

internal static class IdentityServerModuleChecks
{
    internal static readonly ConcurrentDictionary<string, bool> Checks = new();
}

public abstract class IdentityServerModule<TAuthOptions> : AuthModule<TAuthOptions>
    where TAuthOptions : IdentityServerAuthOptions, new()
{
    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TAuthOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        if (Uri.TryCreate(startupOptions.OidcServerUrl, UriKind.Absolute, out var oidcUri))
        {
            if (IdentityServerModuleChecks.Checks.TryAdd(oidcUri.ToString(), true))
            {
                services.AddHealthChecks().AddIdentityServer(oidcUri, $"IdSrv: {oidcUri}");
            }
        }
    }
}

