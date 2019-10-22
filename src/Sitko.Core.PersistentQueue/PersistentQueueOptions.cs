using System;
using System.Collections.Generic;

namespace Sitko.Core.PersistentQueue
{
    public class PersistentQueueOptions
    {
        public readonly List<(string host, int port)> Servers = new List<(string host, int port)>();
        public string ClusterName { get; set; }
        public string ClientName { get; set; }
        public string ConsumerGroupName { get; set; }
        public int ConnectionTimeout { get; set; } = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;
        public bool Verbose { get; set; } = false;
        public int PoolMinSize = 1;
        public int PoolMaxSize = 1024;
        public TimeSpan PruneInterval = TimeSpan.FromSeconds(60);
        public TimeSpan ReconnectInterval = TimeSpan.FromSeconds(60);
        public TimeSpan IdleTime = TimeSpan.FromMinutes(5);
        public bool EmulationMode = false;
    }
}
