using System;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Auth.IdentityServer;

public abstract class IdentityServerModule<TAuthOptions> : AuthModule<TAuthOptions>
    where TAuthOptions : IdentityServerAuthOptions, new()
{
    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TAuthOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        if (Uri.TryCreate(startupOptions.OidcServerUrl, UriKind.Absolute, out var oidcUri))
        {
            services.AddHealthChecks().AddIdentityServer(oidcUri);
        }
    }
}
