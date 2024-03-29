using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Repository.Search;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddSearchRepository(this IHostApplicationBuilder hostApplicationBuilder)
    {
        hostApplicationBuilder.GetSitkoCore().AddSearchRepository();
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder
        AddSearchRepository(this ISitkoCoreApplicationBuilder applicationBuilder) =>
        applicationBuilder.AddModule<SearchRepositoryModule>();
}
