using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Search.OpenSearch;

public class OpenSearchModule : SearchModule<OpenSearchModuleOptions>
{
    public override string OptionsKey => "Search:OpenSearch";

    protected override void ConfigureSearch(IServiceCollection services) =>
        services.AddScoped(typeof(ISearcher<>), typeof(OpenSearchSearcher<>));
}
