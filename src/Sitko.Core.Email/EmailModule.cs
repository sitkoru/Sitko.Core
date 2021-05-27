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

    public abstract class EmailModule<TEmailConfig> : BaseApplicationModule<TEmailConfig>, IEmailModule
        where TEmailConfig : EmailModuleConfig, new()
    {
        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TEmailConfig startupConfig)
        {
            base.ConfigureServices(context, services, startupConfig);
            services.AddViewToStringRenderer<EmailModuleConfig>();
        }
    }
}
