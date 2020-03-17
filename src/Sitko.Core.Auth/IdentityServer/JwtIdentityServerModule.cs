using System;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Auth.IdentityServer
{
    public class JwtIdentityServerModule : IdentityServerModule<JwtAuthOptions>
    {
        public JwtIdentityServerModule(JwtAuthOptions config, Application application) : base(config, application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.Authority = Config.OidcServerUrl;
                options.Audience = Config.JwtAudience;
                options.RequireHttpsMetadata = Config.RequireHttps;
            });
        }

        public override void CheckConfig()
        {
            base.CheckConfig();
            if (string.IsNullOrEmpty(Config.JwtAudience))
            {
                throw new ArgumentException("Oidc jwt audience can't be empty");
            }
        }
    }
}
