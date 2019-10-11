using System;
using Google.Protobuf;

namespace Sitko.Core.PersistentQueue.HostedService
{
    // ReSharper disable once UnusedTypeParameter - need T for dependency injection
    public class PersistedQueueHostedServiceOptions<T> where T : IMessage, new()
    {
        public TimeSpan? StartAt { get; set; }
        public bool All { get; set; }
        public int Workers { get; set; } = 1;
        public bool Durable { get; set; } = true;
        public int MaxInFlight = 200;
        public bool ManualAck = true;
        public int AckWait = (int) TimeSpan.FromSeconds(2).TotalMilliseconds;
    }
}