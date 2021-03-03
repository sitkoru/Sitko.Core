using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Auth.Google
{
    public class GoogleAuthModule : AuthModule<GoogleAuthModuleOptions>
    {
        public GoogleAuthModule(GoogleAuthModuleOptions config, Application application) : base(config, application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);

            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = Config.SignInScheme;
                    options.DefaultChallengeScheme = Config.ChallengeScheme;
                })
                .AddCookie(Config.SignInScheme, options =>
                {
                    options.ExpireTimeSpan = Config.CookieExpire;
                    options.SlidingExpiration = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.None;
                    options.Cookie.IsEssential = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    Config.ConfigureCookie?.Invoke(options.Cookie);
                })
                .AddGoogle(options =>
                {
                    options.ClientId = Config.ClientId;
                    options.ClientSecret = Config.ClientSecret;
                    options.SaveTokens = true;
                    if (Config.Users.Any())
                    {
                        options.Events = new OAuthEvents
                        {
                            OnTicketReceived = receivedContext =>
                            {
                                var email = receivedContext.Principal?.Claims
                                    .FirstOrDefault(c => c.Type == ClaimTypes.Email)
                                    ?.Value;
                                if (email is null)
                                {
                                    receivedContext.Fail($"Empty {email} is not allowed");
                                }
                                else if (!Config.Users.Contains(email))
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

        public override void CheckConfig()
        {
            base.CheckConfig();
            if (string.IsNullOrEmpty(Config.ClientId))
            {
                throw new ArgumentException("ClientId can't be empty", nameof(Config.ClientId));
            }

            if (string.IsNullOrEmpty(Config.ClientSecret))
            {
                throw new ArgumentException("ClientSecret can't be empty", nameof(Config.ClientSecret));
            }

            if (string.IsNullOrEmpty(Config.SignInScheme))
            {
                throw new ArgumentException("SignInScheme can't be empty", nameof(Config.SignInScheme));
            }

            if (string.IsNullOrEmpty(Config.ChallengeScheme))
            {
                throw new ArgumentException("ChallengeScheme can't be empty", nameof(Config.ChallengeScheme));
            }
        }
    }
}
