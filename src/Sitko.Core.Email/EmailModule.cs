using System;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Email
{
    public interface IEmailModule : IApplicationModule
    {
    }

    public abstract class EmailModule<T> : BaseApplicationModule<T>, IEmailModule where T : EmailModuleConfig, new()
    {
        protected EmailModule(T config, Application application) : base(config, application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddHttpContextAccessor();
            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddSingleton(new ViewToStringRendererServiceOptions(Config.Host,
                Config.Scheme));
            services.AddScoped<ViewToStringRendererService>();
        }

        public override void CheckConfig()
        {
            base.CheckConfig();
            if (Config.Host == null)
            {
                throw new ArgumentException("Provide value for host uri to generate absolute urls",
                    nameof(Config.Host));
            }

            if (string.IsNullOrEmpty(Config.Scheme))
            {
                throw new ArgumentException("Provide value for uri scheme to generate absolute urls",
                    nameof(Config.Scheme));
            }
        }
    }
}
