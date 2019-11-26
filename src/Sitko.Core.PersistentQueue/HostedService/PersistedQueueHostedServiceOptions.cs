using System;
using System.Diagnostics.CodeAnalysis;
using Google.Protobuf;

namespace Sitko.Core.PersistentQueue.HostedService
{
    public abstract class PersistedQueueHostedServiceOptions
    {
        public TimeSpan? StartAt { get; set; }
        public bool All { get; set; }
        public int Workers { get; set; } = 1;
        public bool Durable { get; set; } = true;
        public int MaxInFlight = 200;
        public bool ManualAck = true;
        public int AckWait = (int)TimeSpan.FromSeconds(2).TotalMilliseconds;
    }

    [SuppressMessage("ReSharper", "UnusedTypeParameter")]
    public class PersistedQueueHostedServiceOptions<T> : PersistedQueueHostedServiceOptions where T : IMessage, new()
    {
    }
}
