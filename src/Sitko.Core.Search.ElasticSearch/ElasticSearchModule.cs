using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Search.ElasticSearch
{
    public class ElasticSearchModule : SearchModule<ElasticSearchModuleOptions>
    {
        protected override void ConfigureSearch(IServiceCollection services)
        {
            services.AddScoped(typeof(ISearcher<>), typeof(ElasticSearcher<>));
        }

        public override string GetOptionsKey()
        {
            return "Search:Elastic";
        }
    }
}
