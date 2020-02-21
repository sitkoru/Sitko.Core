using Microsoft.AspNetCore.Authorization;
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
            services.AddSingleton<AuthOptions>(Config);
            services.AddAuthorization(options =>
            {
                foreach ((string name, AuthorizationPolicy policy) in Config.Policies)
                {
                    options.AddPolicy(name, policy);
                }
            });
        }

        public virtual void ConfigureAfterUseRouting(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            appBuilder.UseAuthentication()
                .UseAuthorization();
        }
    }
}
