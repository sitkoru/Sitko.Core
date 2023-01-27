using Duende.AccessTokenManagement.OpenIdConnect;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.Auth.IdentityServer.Tokens;

namespace Sitko.Core.Auth.IdentityServer;

public class OidcIdentityServerModule : IdentityServerModule<OidcIdentityServerModuleOptions>
{
    public override string OptionsKey => "Auth:IdentityServer:Oidc";

    protected override void ConfigureAuthentication(AuthenticationBuilder authenticationBuilder,
        OidcIdentityServerModuleOptions startupOptions) =>
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

            if (startupOptions.TokenStoreType != TokenStoreType.None)
            {
                options.EventsType = typeof(OidcEvents);
            }
        });

    protected override void ConfigureCookieOptions(CookieAuthenticationOptions options,
        OidcIdentityServerModuleOptions moduleOptions)
    {
        base.ConfigureCookieOptions(options, moduleOptions);
        if (moduleOptions.TokenStoreType != TokenStoreType.None)
        {
            options.Events.OnSigningOut = async e => { await e.HttpContext.RevokeRefreshTokenAsync(); };
        }
    }

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        OidcIdentityServerModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        if (startupOptions.TokenStoreType != TokenStoreType.None)
        {
            services.AddOpenIdConnectAccessTokenManagement(options =>
            {
                startupOptions.ConfigureUserTokenManagement?.Invoke(options);
            });
            services.AddTransient<OidcEvents>();
            if (startupOptions.TokenStoreType == TokenStoreType.Redis)
            {
                services.AddSingleton<IUserTokenStore, RedisTokenStore>();
            }
            else if (startupOptions.TokenStoreType == TokenStoreType.InMemory)
            {
                services.AddSingleton<IUserTokenStore, InMemoryTokenStore>();
            }
        }
    }
}
