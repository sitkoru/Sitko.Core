namespace Sitko.Core.MessageBus
{
    public class MessageBusModuleConfig
    {
        public int QueueLength { get; set; } = 1000;
        public int WorkersCount { get; set; } = 10;
    }
}
