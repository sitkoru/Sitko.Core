namespace Sitko.Core.Queue.InMemory
{
    public class InMemoryQueueModule : QueueModule<InMemoryQueue, InMemoryQueueModuleOptions>
    {
        public override string OptionsKey => "Queue:InMemory";
    }

    public class InMemoryQueueModuleOptions : QueueModuleOptions
    {
    }
}
