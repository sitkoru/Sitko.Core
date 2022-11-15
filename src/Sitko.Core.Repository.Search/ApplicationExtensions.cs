using JetBrains.Annotations;
using Sitko.Core.App;

namespace Sitko.Core.Repository.Search;

[PublicAPI]
public static class ApplicationExtensions
{
    public static Application AddSearchRepository(this Application application) =>
        application.AddModule<SearchRepositoryModule>();
}

