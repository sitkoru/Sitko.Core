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

    public abstract class AuthModule<TAuthOptions> : BaseApplicationModule<TAuthOptions>, IWebApplicationModule, IAuthModule
        where TAuthOptions : AuthOptions, new()
    {
        public override void ConfigureServices(ApplicationContext context, IServiceCollection services, TAuthOptions startupConfig)
        {
            base.ConfigureServices(context, services, startupConfig);
            services.AddAuthorization(options =>
            {
                foreach ((string name, AuthorizationPolicy policy) in startupConfig.Policies)
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
