using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Repository.Search;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddSearchRepository(this IHostApplicationBuilder hostApplicationBuilder)
    {
        hostApplicationBuilder.AddSitkoCore().AddSearchRepository();
        return hostApplicationBuilder;
    }

    public static SitkoCoreApplicationBuilder
        AddSearchRepository(this SitkoCoreApplicationBuilder applicationBuilder) =>
        applicationBuilder.AddModule<SearchRepositoryModule>();
}
