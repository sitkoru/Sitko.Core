using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Web;

namespace Sitko.Core.Auth
{
    public abstract class AuthModule<T> : BaseApplicationModule<T>, IWebApplicationModule where T : AuthOptions
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddAuthorization(options =>
            {
                foreach (var (name, policy) in Config.Policies)
                {
                    options.AddPolicy(name, policy);
                }
            });
            services.AddHealthChecks().AddIdentityServer(new Uri(Config.OidcServerUrl));
        }

        protected override void CheckConfig()
        {
            base.CheckConfig();
            if (string.IsNullOrEmpty(Config.OidcServerUrl))
            {
                throw new ArgumentException("Oidc servder url can't be empty");
            }
        }

        public virtual void ConfigureAfterUseRouting(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            appBuilder.UseAuthentication()
                .UseAuthorization();
        }
    }
}
