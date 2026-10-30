using Duende.AccessTokenManagement.OpenIdConnect;
using Duende.IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.Auth.OIDC.Tokens;

namespace Sitko.Core.Auth.OIDC;

public class OidcAuthModule : OidcProviderModule<OidcAuthModuleOptions>
{
    public override string OptionsKey => "Auth:OIDC:Client";

    protected override void ConfigureAuthentication(AuthenticationBuilder authenticationBuilder,
        OidcAuthModuleOptions startupOptions) =>
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

            startupOptions.ConfigureOpenIdConnectOptions?.Invoke(options);
        });

    protected override bool RequiresRedis(OidcAuthModuleOptions options) =>
        base.RequiresRedis(options) || options.TokenStoreType == TokenStoreType.Redis;

    protected override void ConfigureCookieOptions(CookieAuthenticationOptions options,
        OidcAuthModuleOptions moduleOptions)
    {
        base.ConfigureCookieOptions(options, moduleOptions);
        if (moduleOptions.TokenStoreType != TokenStoreType.None)
        {
            options.Events.OnSigningOut = async e => { await e.HttpContext.RevokeRefreshTokenAsync(); };
        }
    }

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        OidcAuthModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        if (startupOptions.TokenStoreType != TokenStoreType.None)
        {
            services.AddOpenIdConnectAccessTokenManagement(options =>
            {
                startupOptions.ConfigureUserTokenManagement?.Invoke(options);
            });
            services.AddTransient<OidcEvents>();
            _ = services.PostConfigure<OpenIdConnectOptions>(startupOptions.ChallengeScheme, options =>
            {
                if (options.EventsType is not null)
                {
                    throw new InvalidOperationException("Use OpenIdConnectOptions.Events instead of EventsType with managed token storage.");
                }

                var validated = options.Events.OnTokenValidated;
                options.Events.OnTokenValidated = async context =>
                {
                    await validated(context);
                    if (context.Result is null)
                    {
                        await context.HttpContext.RequestServices.GetRequiredService<OidcEvents>().TokenValidated(context);
                    }
                };
                var received = options.Events.OnTicketReceived;
                options.Events.OnTicketReceived = async context =>
                {
                    await received(context);
                    if (context.Result is null)
                    {
                        await context.HttpContext.RequestServices.GetRequiredService<OidcEvents>().TicketReceived(context);
                    }
                };
            });
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
