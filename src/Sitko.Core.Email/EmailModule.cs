using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.App.Web.Razor;

namespace Sitko.Core.Email
{
    public interface IEmailModule : IApplicationModule
    {
    }

    public abstract class EmailModule<TEmailConfig> : BaseApplicationModule<TEmailConfig>, IEmailModule
        where TEmailConfig : EmailModuleOptions, new()
    {
        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TEmailConfig startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddViewToStringRenderer<EmailModuleOptions>();
        }
    }
}
