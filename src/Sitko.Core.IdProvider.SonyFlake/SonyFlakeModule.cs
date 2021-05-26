using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.IdProvider.SonyFlake
{
    public class SonyFlakeModule : BaseApplicationModule<SonyFlakeModuleConfig>
    {
        public SonyFlakeModule(Application application) : base(application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddHttpClient<IIdProvider, SonyFlakeIdProvider>(client =>
            {
                client.BaseAddress = new Uri(GetConfig().Uri);
            });
        }
    }
}
