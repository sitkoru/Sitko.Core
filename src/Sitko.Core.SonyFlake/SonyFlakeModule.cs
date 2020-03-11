using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.SonyFlake
{
    public class SonyFlakeModule : BaseApplicationModule<SonyFlakeModuleConfig>
    {
        public SonyFlakeModule(SonyFlakeModuleConfig config, Application application) : base(config,application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddHttpClient<IIdProvider, SonyFlakeIdProvider>(client =>
            {
                client.BaseAddress = Config.SonyflakeUri;
            });
        }
    }
}
