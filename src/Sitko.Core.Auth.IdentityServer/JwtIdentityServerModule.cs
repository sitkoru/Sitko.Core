using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Auth.IdentityServer;

public class JwtIdentityServerModule : IdentityServerModule<JwtIdentityServerModuleOptions>
{
    public override string OptionsKey => "Auth:IdentityServer:Jwt";

    protected override void ConfigureAuthentication(AuthenticationBuilder authenticationBuilder,
        JwtIdentityServerModuleOptions startupOptions)
    {
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        authenticationBuilder.AddJwtBearer(options =>
        {
            options.Authority = startupOptions.OidcServerUrl;
            options.Audience = startupOptions.JwtAudience;
            options.RequireHttpsMetadata = startupOptions.RequireHttps;
            if (startupOptions.ValidIssuers.Length > 0)
            {
                options.TokenValidationParameters.ValidIssuers = startupOptions.ValidIssuers;
            }

            startupOptions.ConfigureJwtBearerOptions?.Invoke(options);
        });
    }
}
