using System;
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
    public class OidcIdentityServerModule : IdentityServerModule<OidcAuthOptions>
    {
        public override string GetConfigKey()
        {
            return "Auth:IdentityServer:Oidc";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            OidcAuthOptions startupConfig)
        {
            base.ConfigureServices(context, services, startupConfig);

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = startupConfig.SignInScheme;
                    options.DefaultChallengeScheme = startupConfig.ChallengeScheme;
                })
                .AddCookie(startupConfig.SignInScheme, options =>
                {
                    options.ExpireTimeSpan = startupConfig.ExpireTimeSpan;
                    options.SlidingExpiration = startupConfig.SlidingExpiration;
                })
                .AddOpenIdConnect(startupConfig.ChallengeScheme, options =>
                {
                    options.SignInScheme = startupConfig.SignInScheme;

                    options.Authority = startupConfig.OidcServerUrl;
                    options.RequireHttpsMetadata = startupConfig.RequireHttps;

                    options.ClientId = startupConfig.OidcClientId;
                    options.ClientSecret = startupConfig.OidcClientSecret;
                    options.ResponseType = startupConfig.ResponseType;
                    options.UsePkce = startupConfig.UsePkce;

                    options.SaveTokens = startupConfig.SaveTokens;
                    options.GetClaimsFromUserInfoEndpoint = startupConfig.GetClaimsFromUserInfoEndpoint;

                    options.Scope.Add(OidcConstants.StandardScopes.OfflineAccess);
                    if (startupConfig.OidcScopes.Any())
                    {
                        foreach (string scope in startupConfig.OidcScopes)
                        {
                            options.Scope.Add(scope);
                        }
                    }
                });

            if (startupConfig.EnableRedisDataProtection)
            {
                services.AddDataProtection().PersistKeysToStackExchangeRedis(() =>
                    {
                        var redis = ConnectionMultiplexer
                            .Connect($"{startupConfig.RedisHost}:{startupConfig.RedisPort}");
                        return redis.GetDatabase(startupConfig.RedisDb);
                    }, $"{context.Environment.ApplicationName}-DP")
                    .SetApplicationName(context.Environment.ApplicationName)
                    .SetDefaultKeyLifetime(startupConfig.DataProtectionLifeTime);
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
