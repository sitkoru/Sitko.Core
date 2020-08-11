using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Auth.IdentityServer
{
    public abstract class IdentityServerModule<T> : AuthModule<T> where T : IdentityServerAuthOptions, new()
    {
        protected IdentityServerModule(T config, Application application) : base(config, application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            if (Uri.TryCreate(Config.OidcServerUrl, UriKind.Absolute, out var oidcUri))
            {
                services.AddHealthChecks().AddIdentityServer(oidcUri);
            }
        }

        public override void CheckConfig()
        {
            base.CheckConfig();
            if (string.IsNullOrEmpty(Config.OidcServerUrl))
            {
                throw new ArgumentException("Oidc servder url can't be empty");
            }
        }
    }
}
