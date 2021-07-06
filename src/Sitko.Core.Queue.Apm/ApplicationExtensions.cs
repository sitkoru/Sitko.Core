using Sitko.Core.App;

namespace Sitko.Core.Queue.Apm
{
    public static class ApplicationExtensions
    {
        public static Application AddQueueElasticApm(this Application application)
        {
            return application.AddModule<QueueElasticApmModule>();
        }
    }
}
