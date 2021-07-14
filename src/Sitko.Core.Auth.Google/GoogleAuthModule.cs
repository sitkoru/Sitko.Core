using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Auth.Google
{
    using System;

    public class GoogleAuthModule : AuthModule<GoogleAuthModuleOptions>
    {
        public override string OptionsKey => "Auth:Google";

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            GoogleAuthModuleOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = startupOptions.SignInScheme;
                    options.DefaultChallengeScheme = startupOptions.ChallengeScheme;
                })
                .AddCookie(startupOptions.SignInScheme, options =>
                {
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(startupOptions.CookieExpireInMinutes);
                    options.SlidingExpiration = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.None;
                    options.Cookie.IsEssential = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    startupOptions.ConfigureCookie?.Invoke(options.Cookie);
                })
                .AddGoogle(options =>
                {
                    options.ClientId = startupOptions.ClientId;
                    options.ClientSecret = startupOptions.ClientSecret;
                    options.SaveTokens = true;
                    if (startupOptions.Users.Any())
                    {
                        options.Events = new OAuthEvents
                        {
                            OnTicketReceived = receivedContext =>
                            {
                                var config = GetOptions(receivedContext.HttpContext.RequestServices);
                                var email = receivedContext.Principal?.Claims
                                    .FirstOrDefault(c => c.Type == ClaimTypes.Email)
                                    ?.Value;
                                if (email is null)
                                {
                                    receivedContext.Fail($"Empty {email} is not allowed");
                                }
                                else if (!config.Users.Contains(email))
                                {
                                    receivedContext.Fail($"User {email} is not allowed");
                                }
                                else
                                {
                                    receivedContext.Success();
                                }

                                return Task.CompletedTask;
                            }
                        };
                    }
                });
        }
    }
}
