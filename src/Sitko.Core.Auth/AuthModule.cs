using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.App.Web;
using StackExchange.Redis;

namespace Sitko.Core.Auth
{
    public interface IAuthModule : IApplicationModule
    {}

    public abstract class AuthModule<TAuthOptions> : BaseApplicationModule<TAuthOptions>, IWebApplicationModule,
        IAuthModule
        where TAuthOptions : AuthOptions, new()
    {
        public virtual void ConfigureAfterUseRouting(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            appBuilder.UseAuthentication()
                .UseAuthorization();
            appBuilder.UseMiddleware<AuthorizationMiddleware<TAuthOptions>>();
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TAuthOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            var authenticationBuilder = services.AddAuthentication(options =>
            {
                options.DefaultScheme = startupOptions.SignInScheme;
                options.DefaultChallengeScheme = startupOptions.ChallengeScheme;
            });
            ConfigureAuthentication(authenticationBuilder, startupOptions);
            if (startupOptions.RequiresCookie)
            {
                services.AddAuthentication().AddCookie(startupOptions.SignInScheme, options =>
                {
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(startupOptions.CookieExpireInMinutes);
                    options.SlidingExpiration = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.None;
                    options.Cookie.IsEssential = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    startupOptions.ConfigureCookie?.Invoke(options.Cookie);
                });
            }

            services.AddAuthorization(options =>
            {
                foreach ((string name, AuthorizationPolicy policy) in startupOptions.Policies)
                {
                    options.AddPolicy(name, policy);
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

        protected abstract void ConfigureAuthentication(AuthenticationBuilder authenticationBuilder,
            TAuthOptions startupOptions);
    }
}
