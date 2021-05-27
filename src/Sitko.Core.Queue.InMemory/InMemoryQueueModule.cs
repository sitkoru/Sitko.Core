using Sitko.Core.App;

namespace Sitko.Core.Queue.InMemory
{
    public class InMemoryQueueModule : QueueModule<InMemoryQueue, InMemoryQueueModuleOptions>
    { 
        public override string GetOptionsKey()
        {
            return "Queue:InMemory";
        }
    }

    public class InMemoryQueueModuleOptions : QueueModuleOptions
    {
    }
}
