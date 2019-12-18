using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Email
{
    public abstract class EmailModule<T> : BaseApplicationModule<T> where T : EmailModuleConfig
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddHttpContextAccessor();
            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddSingleton(new ViewToStringRendererServiceOptions(Config.Host,
                Config.Scheme));
            services.AddScoped<ViewToStringRendererService>();
            services.AddScoped<IMailSender, FluentMailSender>();

            var builder = services.AddFluentEmail(Config.From);
            ConfigureBuilder(builder);
        }

        protected abstract void ConfigureBuilder(FluentEmailServicesBuilder builder);
    }

    public abstract class EmailModuleConfig
    {
        protected EmailModuleConfig(string from, string host, string scheme)
        {
            From = from;
            Host = new HostString(host);
            Scheme = scheme;
        }

        public HostString Host { get; }
        public string Scheme { get; }
        public string From { get; }
    }
}
