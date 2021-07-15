using Sitko.Core.App;

namespace Sitko.Core.Repository.Search
{
    using JetBrains.Annotations;

    [PublicAPI]
    public static class ApplicationExtensions
    {
        public static Application AddSearchRepository(this Application application) =>
            application.AddModule<SearchRepositoryModule>();
    }
}
