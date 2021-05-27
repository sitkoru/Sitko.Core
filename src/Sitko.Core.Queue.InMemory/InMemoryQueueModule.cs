using Sitko.Core.App;

namespace Sitko.Core.Queue.InMemory
{
    public class InMemoryQueueModule : QueueModule<InMemoryQueue, InMemoryQueueModuleConfig>
    { 
        public override string GetConfigKey()
        {
            return "Queue:InMemory";
        }
    }

    public class InMemoryQueueModuleConfig : QueueModuleConfig
    {
    }
}
