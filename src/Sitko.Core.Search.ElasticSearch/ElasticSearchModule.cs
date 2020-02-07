using System;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Search.ElasticSearch
{
    public class ElasticSearchModule : SearchModule<ElasticSearchModuleConfig>
    {
        protected override void CheckConfig()
        {
            if (string.IsNullOrEmpty(Config.Url))
            {
                throw new ArgumentException("Elastic url is empty");
            }
        }

        protected override void ConfigureSearch(IServiceCollection services)
        {
            services.AddScoped(typeof(ISearcher<>), typeof(ElasticSearcher<>));
        }
    }
}
