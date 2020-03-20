using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.App.Web;

namespace Sitko.Core.Auth
{
    public interface IAuthModule : IApplicationModule
    {
    }

    public abstract class AuthModule<T> : BaseApplicationModule<T>, IWebApplicationModule, IAuthModule
        where T : AuthOptions, new()
    {
        protected AuthModule(T config, Application application) : base(config, application)
        {
        }

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
