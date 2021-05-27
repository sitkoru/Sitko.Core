using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.MediatR
{
    public interface IMediatRModule
    {
    }

    public class MediatRModule<TAssembly> : BaseApplicationModule<MediatRModuleConfig<TAssembly>>,
        IMediatRModule
    {
        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            MediatRModuleConfig<TAssembly> startupConfig)
        {
            base.ConfigureServices(context, services, startupConfig);
            services.AddMediatR(startupConfig.Assemblies.ToArray());
        }

        public override string GetConfigKey()
        {
            return "MediatR";
        }
    }
}
