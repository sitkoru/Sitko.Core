using System;

namespace Sitko.Core.PersistentQueue
{
    public class PersistentQueueModuleOptions
    {
        public string ConsumerGroupName { get; set; }
        public int ConnectionTimeout { get; set; } = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;
        public bool Verbose { get; set; } = false;
    }
}
