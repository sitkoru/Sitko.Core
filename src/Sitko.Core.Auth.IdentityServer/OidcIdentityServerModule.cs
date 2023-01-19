using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.Auth.IdentityServer.Tokens;

namespace Sitko.Core.Auth.IdentityServer;

public class OidcIdentityServerModule : IdentityServerModule<OidcIdentityServerModuleOptions>
{
    public override string OptionsKey => "Auth:IdentityServer:Oidc";

    protected override void ConfigureAuthentication(AuthenticationBuilder authenticationBuilder,
        OidcIdentityServerModuleOptions startupOptions)
    {
        authenticationBuilder.AddOpenIdConnect(startupOptions.ChallengeScheme, options =>
        {
            options.SignInScheme = startupOptions.SignInScheme;

            options.Authority = startupOptions.OidcServerUrl;
            options.RequireHttpsMetadata = startupOptions.RequireHttps;

            options.ClientId = startupOptions.OidcClientId;
            options.ClientSecret = startupOptions.OidcClientSecret;
            options.ResponseType = startupOptions.ResponseType;
            options.UsePkce = startupOptions.UsePkce;

            options.SaveTokens = startupOptions.SaveTokens;
            options.GetClaimsFromUserInfoEndpoint = startupOptions.GetClaimsFromUserInfoEndpoint;

            options.Scope.Add(OidcConstants.StandardScopes.OfflineAccess);
            if (startupOptions.OidcScopes.Any())
            {
                foreach (var scope in startupOptions.OidcScopes)
                {
                    options.Scope.Add(scope);
                }
            }
        });
        if (startupOptions.AutoRefreshTokens)
        {
            authenticationBuilder.AddAutomaticTokenManagement(options =>
            {
                startupOptions.ConfigureAutoRefreshTokens?.Invoke(options);
            });
        }
    }

    protected override void ConfigureCookieOptions(CookieAuthenticationOptions options,
        OidcIdentityServerModuleOptions moduleOptions)
    {
        base.ConfigureCookieOptions(options, moduleOptions);
        if (moduleOptions.AutoRefreshTokens)
        {
            options.EventsType = typeof(AutomaticTokenManagementCookieEvents);
        }
    }
}
