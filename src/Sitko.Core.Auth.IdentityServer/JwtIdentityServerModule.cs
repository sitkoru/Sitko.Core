using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Auth.IdentityServer
{
    public class JwtIdentityServerModule : IdentityServerModule<JwtAuthOptions>
    {
        public override string GetConfigKey()
        {
            return "Auth:IdentityServer:Jwt";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            JwtAuthOptions startupConfig)
        {
            base.ConfigureServices(context, services, startupConfig);
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.Authority = startupConfig.OidcServerUrl;
                options.Audience = startupConfig.JwtAudience;
                options.RequireHttpsMetadata = startupConfig.RequireHttps;
            });
        }
    }
}
