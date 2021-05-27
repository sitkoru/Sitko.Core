using System;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Auth.IdentityServer
{
    public abstract class IdentityServerModule<TAuthOptions> : AuthModule<TAuthOptions>
        where TAuthOptions : IdentityServerAuthOptions, new()
    {
        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TAuthOptions startupConfig)
        {
            base.ConfigureServices(context, services, startupConfig);
            if (Uri.TryCreate(startupConfig.OidcServerUrl, UriKind.Absolute, out var oidcUri))
            {
                services.AddHealthChecks().AddIdentityServer(oidcUri);
            }
        }
    }
}
