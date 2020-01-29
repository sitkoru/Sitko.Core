using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sitko.Core.Email
{
    public abstract class FluentEmailModule<T> : EmailModule<T> where T : FluentEmailModuleConfig
    {
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
