using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.StackExchangeRedis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.App.Web;
using StackExchange.Redis;

namespace Sitko.Core.Auth;

public interface IAuthModule : IApplicationModule;

public abstract class AuthModule<TAuthOptions> : BaseApplicationModule<TAuthOptions>, IAuthApplicationModule,
    IAuthModule
    where TAuthOptions : AuthOptions, new()
{
    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TAuthOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        if (startupOptions.RequiresAuthentication)
        {
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
        }

        services.AddAuthorization(options =>
        {
            foreach (var (name, policy) in startupOptions.Policies)
            {
                options.AddPolicy(name, policy);
            }

            if (!string.IsNullOrEmpty(startupOptions.ForcePolicy))
            {
                options.FallbackPolicy = startupOptions.Policies
                    .FirstOrDefault(pair => pair.Key == startupOptions.ForcePolicy).Value;
            }
        });
        _ = services.AddSingleton(_ => new AuthRedisConnection<TAuthOptions>(startupOptions));
        _ = services.AddKeyedSingleton<IDatabase>(typeof(TAuthOptions), static (provider, _) =>
            provider.GetRequiredService<AuthRedisConnection<TAuthOptions>>().Database);
        if (startupOptions.EnableRedisDataProtection)
        {
            _ = services.AddDataProtection()
                .SetApplicationName(applicationContext.Name)
                .SetDefaultKeyLifetime(TimeSpan.FromMinutes(startupOptions.DataProtectionLifeTimeInMinutes));
            _ = services.AddOptions<KeyManagementOptions>().Configure<AuthRedisConnection<TAuthOptions>>(
                (options, redis) => options.XmlRepository =
                    new RedisXmlRepository(() => redis.Database, $"{applicationContext.Name}-DP"));
        }
    }

    protected virtual void ConfigureCookieOptions(CookieAuthenticationOptions options, TAuthOptions moduleOptions)
    {
    }

    protected abstract void ConfigureAuthentication(AuthenticationBuilder authenticationBuilder,
        TAuthOptions startupOptions);
}
