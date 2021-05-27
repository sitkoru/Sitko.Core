using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Search.ElasticSearch
{
    public class ElasticSearchModule : SearchModule<ElasticSearchModuleConfig>
    {
        protected override void ConfigureSearch(IServiceCollection services)
        {
            services.AddScoped(typeof(ISearcher<>), typeof(ElasticSearcher<>));
        }

        public override string GetConfigKey()
        {
            return "Search:Elastic";
        }
    }
}
