using System.Linq;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Auth.IdentityServer
{
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
                    foreach (string scope in startupOptions.OidcScopes)
                    {
                        options.Scope.Add(scope);
                    }
                }
            });
    }
}
