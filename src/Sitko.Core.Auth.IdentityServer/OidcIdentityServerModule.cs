using System.Linq;
using IdentityModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using StackExchange.Redis;

namespace Sitko.Core.Auth.IdentityServer
{
    using System;

    public class OidcIdentityServerModule : IdentityServerModule<OidcIdentityServerModuleOptions>
    {
        public override string OptionsKey => "Auth:IdentityServer:Oidc";

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            OidcIdentityServerModuleOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = startupOptions.SignInScheme;
                    options.DefaultChallengeScheme = startupOptions.ChallengeScheme;
                })
                .AddCookie(startupOptions.SignInScheme, options =>
                {
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(startupOptions.ExpireTimeSpanInMinutes);
                    options.SlidingExpiration = startupOptions.SlidingExpiration;
                })
                .AddOpenIdConnect(startupOptions.ChallengeScheme, options =>
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

            if (startupOptions.EnableRedisDataProtection)
            {
                services.AddDataProtection().PersistKeysToStackExchangeRedis(() =>
                    {
                        var redis = ConnectionMultiplexer
                            .Connect($"{startupOptions.RedisHost}:{startupOptions.RedisPort}");
                        return redis.GetDatabase(startupOptions.RedisDb);
                    }, $"{context.Environment.ApplicationName}-DP")
                    .SetApplicationName(context.Environment.ApplicationName)
                    .SetDefaultKeyLifetime(TimeSpan.FromMinutes(startupOptions.DataProtectionLifeTimeInMinutes));
            }
        }

        public override void ConfigureAfterUseRouting(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            base.ConfigureAfterUseRouting(configuration, environment, appBuilder);
            appBuilder.UseMiddleware<AuthorizationMiddleware>();
        }
    }
}
