using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Repository.Search
{
    public class SearchRepositoryModule : BaseApplicationModule
    {
        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            BaseApplicationModuleConfig startupConfig)
        {
            base.ConfigureServices(context, services, startupConfig);
            services.AddScoped<IRepositoryFilter, SearchRepositoryFilter>();
            services.AddScoped<RepositoryIndexer>();
        }

        public override string GetConfigKey()
        {
            return "Search:Repository";
        }
    }
}
