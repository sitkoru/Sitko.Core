using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.App.Web.Razor;

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
            services.AddViewToStringRenderer(Config.Host, Config.Scheme);
        }

        public override void CheckConfig()
        {
            base.CheckConfig();

            if (string.IsNullOrEmpty(Config.Scheme))
            {
                throw new ArgumentException("Provide value for uri scheme to generate absolute urls",
                    nameof(Config.Scheme));
            }
        }
    }
}
