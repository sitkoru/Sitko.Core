using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Repository.Search
{
    public class SearchRepositoryModule : BaseApplicationModule
    {
        public SearchRepositoryModule(BaseApplicationModuleConfig config, Application application) : base(config,
            application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddScoped<IRepositoryFilter, SearchRepositoryFilter>();
        }
    }
}
