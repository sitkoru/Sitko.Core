using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.MessageBus
{
    public class MessageBusModule : BaseApplicationModule<MessageBusModuleConfig>
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddSingleton<IMessageBus, MessageBus>();
            services.AddHostedService<MessageBusService>();
        }
    }
}
