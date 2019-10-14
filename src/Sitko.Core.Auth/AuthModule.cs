using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Web;

namespace Sitko.Core.Auth
{
    public class AuthModule : BaseApplicationModule<AuthOptions>, IWebApplicationModule
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

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
                    options.RequireHttpsMetadata = false;

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
            services.AddHealthChecks().AddIdentityServer(new Uri(Config.OidcServerUrl));
            if (Config.EnableJwt)
            {
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
                {
                    options.Authority = Config.OidcServerUrl;
                    options.Audience = Config.OidcServerUrl + "/resources";
                    options.RequireHttpsMetadata = Config.RequireHttps;
                });
            }

            //ConfigureAuth(defaultPolicy, services);
            services.AddAuthorization(options =>
            {
                foreach (var (name, policy) in Config.Policies)
                {
                    options.AddPolicy(name, policy);
                }
            });
        }

        public void ConfigureBeforeUseRouting(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
        }

        public Task ApplicationStarted(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            return Task.CompletedTask;
        }

        public Task ApplicationStopping(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            return Task.CompletedTask;
        }

        public Task ApplicationStopped(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            return Task.CompletedTask;
        }

        public void ConfigureEndpoints(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder, IEndpointRouteBuilder endpoints)
        {
        }


        public void ConfigureAfterUseRouting(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            appBuilder.UseAuthentication()
                .UseAuthorization()
                .UseMiddleware<AuthorizationMiddleware>()
                .UseMiddleware<UserMiddleware>();
        }

        public void ConfigureWebHostDefaults(IWebHostBuilder webHostBuilder)
        {
        }

        public void ConfigureWebHost(IWebHostBuilder webHostBuilder)
        {
        }
    }
}
