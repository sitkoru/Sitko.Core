using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Search.ElasticSearch;

public class ElasticSearchModule : SearchModule<ElasticSearchModuleOptions>
{
    public override string OptionsKey => "Search:Elastic";

    protected override void ConfigureSearch(IServiceCollection services) =>
        services.AddScoped(typeof(ISearcher<>), typeof(ElasticSearcher<>));
}

