using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.MessageBus
{
    public interface IMessageBusModule
    {
    }

    public class MessageBusModule<TAssembly> : BaseApplicationModule<MessageBusModuleConfig<TAssembly>>,
        IMessageBusModule
    {
        public MessageBusModule(MessageBusModuleConfig<TAssembly> config, Application application) : base(config,
            application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddMediatR(Config.Assemblies.ToArray());
        }
    }
}
