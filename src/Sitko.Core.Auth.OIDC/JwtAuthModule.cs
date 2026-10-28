using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Auth.OIDC;

public class JwtAuthModule : OidcProviderModule<JwtAuthModuleOptions>
{
    public override string OptionsKey => "Auth:OIDC:Jwt";

    protected override void ConfigureAuthentication(AuthenticationBuilder authenticationBuilder,
        JwtAuthModuleOptions startupOptions)
    {
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        authenticationBuilder.AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
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
