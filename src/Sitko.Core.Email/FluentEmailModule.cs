using System.Net.Mail;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Email
{
    public abstract class FluentEmailModule<TEmailConfig> : EmailModule<TEmailConfig>
        where TEmailConfig : FluentEmailModuleConfig, new()
    {
        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TEmailConfig startupConfig)
        {
            base.ConfigureServices(context, services, startupConfig);
            services.AddScoped<IMailSender, FluentMailSender>();
            var address = new MailAddress(startupConfig.From);
            var builder = services.AddFluentEmail(address.Address, address.DisplayName);
            ConfigureBuilder(builder, startupConfig);
        }

        protected abstract void ConfigureBuilder(FluentEmailServicesBuilder builder,
            TEmailConfig config);
    }
}
