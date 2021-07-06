using Sitko.Core.App;

namespace Sitko.Core.Repository.Search
{
    public static class ApplicationExtensions
    {
        public static Application AddSearchRepository(this Application application)
        {
            return application.AddModule<SearchRepositoryModule>();
        }
    }
}
