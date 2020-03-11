using Sitko.Core.App;

namespace Sitko.Core.Queue.InMemory
{
    public class InMemoryQueueModule : QueueModule<InMemoryQueue, InMemoryQueueModuleConfig>
    {
        public InMemoryQueueModule(InMemoryQueueModuleConfig config, Application application) : base(config, application)
        {
        }
    }

    public class InMemoryQueueModuleConfig : QueueModuleConfig
    {
    }
}
