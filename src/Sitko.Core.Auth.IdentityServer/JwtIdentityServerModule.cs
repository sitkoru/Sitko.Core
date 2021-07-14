using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Auth.IdentityServer
{
    public class JwtIdentityServerModule : IdentityServerModule<JwtIdentityServerModuleOptions>
    {
        public override string OptionsKey => "Auth:IdentityServer:Jwt";

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            JwtIdentityServerModuleOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.Authority = startupOptions.OidcServerUrl;
                options.Audience = startupOptions.JwtAudience;
                options.RequireHttpsMetadata = startupOptions.RequireHttps;
            });
        }
    }
}
