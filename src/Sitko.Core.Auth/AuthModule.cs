using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.App.Web;
using StackExchange.Redis;

namespace Sitko.Core.Auth;

public interface IAuthModule : IApplicationModule
{
}

public abstract class AuthModule<TAuthOptions> : BaseApplicationModule<TAuthOptions>, IWebApplicationModule,
    IAuthModule
    where TAuthOptions : AuthOptions, new()
{
    public virtual void ConfigureAfterUseRouting(IApplicationContext applicationContext,
        IApplicationBuilder appBuilder)
    {
        appBuilder.UseAuthentication()
            .UseAuthorization();
        appBuilder.UseMiddleware<AuthorizationMiddleware<TAuthOptions>>();
    }

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TAuthOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        var authenticationBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = startupOptions.SignInScheme;
            options.DefaultChallengeScheme = startupOptions.ChallengeScheme;
        });
        if (startupOptions.RequiresCookie)
        {
            authenticationBuilder.AddCookie(startupOptions.SignInScheme, options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromMinutes(startupOptions.CookieExpireInMinutes);
                options.SlidingExpiration = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                ConfigureCookieOptions(options, startupOptions);
                startupOptions.ConfigureCookie?.Invoke(options.Cookie);
            });
        }

        ConfigureAuthentication(authenticationBuilder, startupOptions);


        services.AddAuthorization(options =>
        {
            foreach (var (name, policy) in startupOptions.Policies)
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
                }, $"{applicationContext.Name}-DP")
                .SetApplicationName(applicationContext.Name)
                .SetDefaultKeyLifetime(TimeSpan.FromMinutes(startupOptions.DataProtectionLifeTimeInMinutes));
        }
    }

    protected virtual void ConfigureCookieOptions(CookieAuthenticationOptions options, TAuthOptions moduleOptions)
    {
    }

    protected abstract void ConfigureAuthentication(AuthenticationBuilder authenticationBuilder,
        TAuthOptions startupOptions);
}

