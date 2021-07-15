using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Repository.Search
{
    public class SearchRepositoryModule : BaseApplicationModule
    {
        public override string OptionsKey => "Search:Repository";

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            BaseApplicationModuleOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddScoped<IRepositoryFilter, SearchRepositoryFilter>();
            services.AddScoped<RepositoryIndexer>();
        }
    }
}
