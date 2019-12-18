using System;
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
            if (string.IsNullOrEmpty(from))
            {
                throw new ArgumentException("Provide value for from address", nameof(from));
            }

            From = from;

            if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentException("Provide value for host uri to generate absolute urls", nameof(host));
            }

            Host = new HostString(host);

            if (string.IsNullOrEmpty(scheme))
            {
                throw new ArgumentException("Provide value for uri scheme to generate absolute urls", nameof(scheme));
            }

            Scheme = scheme;
        }

        public HostString Host { get; }
        public string Scheme { get; }
        public string From { get; }
    }
}
