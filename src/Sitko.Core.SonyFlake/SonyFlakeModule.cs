using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.SonyFlake
{
    public class SonyFlakeModule: BaseApplicationModule<SonyFlakeModuleConfig>
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddHttpClient()
                .AddSingleton<IIdProvider, SonyFlakeIdProvider>();
        }
    }
}
