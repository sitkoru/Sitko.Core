using System.Net.Mail;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Email
{
    public abstract class FluentEmailModule<TEmailConfig> : EmailModule<TEmailConfig>
        where TEmailConfig : FluentEmailModuleOptions, new()
    {
        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TEmailConfig startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddScoped<IMailSender, FluentMailSender>();
            var address = new MailAddress(startupOptions.From);
            var builder = services.AddFluentEmail(address.Address, address.DisplayName);
            ConfigureBuilder(builder, startupOptions);
        }

        protected abstract void ConfigureBuilder(FluentEmailServicesBuilder builder,
            TEmailConfig config);
    }
}
