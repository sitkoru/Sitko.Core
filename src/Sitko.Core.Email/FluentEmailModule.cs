using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Email
{
    public abstract class FluentEmailModule<T> : EmailModule<T> where T : FluentEmailModuleConfig
    {
        protected FluentEmailModule(T config, Application application) : base(config, application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);

            services.AddScoped<IMailSender, FluentMailSender>();
            var builder = services.AddFluentEmail(Config.From);
            ConfigureBuilder(builder);
        }

        protected abstract void ConfigureBuilder(FluentEmailServicesBuilder builder);
    }
}
