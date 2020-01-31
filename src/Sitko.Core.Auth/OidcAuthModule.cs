using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace Sitko.Core.Auth
{
    public class OidcAuthModule : AuthModule<OidcAuthOptions>
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = "Cookies";
                    options.DefaultChallengeScheme = "oidc";
                })
                .AddCookie("Cookies", options =>
                {
                    options.ExpireTimeSpan = TimeSpan.FromDays(30);
                    options.SlidingExpiration = true;
                })
                .AddOpenIdConnect("oidc", options =>
                {
                    options.SignInScheme = "Cookies";

                    options.Authority = Config.OidcServerUrl;
                    options.RequireHttpsMetadata = Config.RequireHttps;

                    options.ClientId = Config.OidcClientId;
                    options.ClientSecret = Config.OidcClientSecret;
                    options.ResponseType = "code id_token";

                    options.SaveTokens = true;
                    options.GetClaimsFromUserInfoEndpoint = true;

                    options.Scope.Add("offline_access");
                    if (Config.OidcScopes.Any())
                    {
                        foreach (string scope in Config.OidcScopes)
                        {
                            options.Scope.Add(scope);
                        }
                    }
                });

            if (Config.EnableRedisDataProtection)
            {
                services.AddDataProtection().PersistKeysToStackExchangeRedis(() =>
                    {
                        var redis = ConnectionMultiplexer
                            .Connect($"{Config.RedisHost}:{Config.RedisPort}");
                        return redis.GetDatabase(Config.RedisDb);
                    }, $"{environment.ApplicationName}-DP").SetApplicationName(environment.ApplicationName)
                    .SetDefaultKeyLifetime(Config.DataProtectionLifeTime);
            }
        }

        public override void ConfigureAfterUseRouting(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            base.ConfigureAfterUseRouting(configuration, environment, appBuilder);
            appBuilder.UseMiddleware<AuthorizationMiddleware>();
        }

        protected override void CheckConfig()
        {
            base.CheckConfig();

            if (string.IsNullOrEmpty(Config.OidcClientId))
            {
                throw new ArgumentException("Oidc client id can't be empty");
            }
            
            if (string.IsNullOrEmpty(Config.OidcClientSecret))
            {
                throw new ArgumentException("Oidc client secret can't be empty");
            }
            
            if (Config.EnableRedisDataProtection)
            {
                if (string.IsNullOrEmpty(Config.RedisHost))
                {
                    throw new ArgumentException("Redis host can't be empty when Redis Data protection enabled");
                }
                
                if (Config.RedisPort == 0)
                {
                    throw new ArgumentException("Redis port can't be empty when Redis Data protection enabled");
                }
            }
        }
    }
}
