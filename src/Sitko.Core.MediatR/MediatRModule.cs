using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.MediatR
{
    public interface IMediatRModule
    {
    }

    public class MediatRModule<TAssembly> : BaseApplicationModule<MediatRModuleConfig<TAssembly>>,
        IMediatRModule
    {
        public MediatRModule(MediatRModuleConfig<TAssembly> config, Application application) : base(config,
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
