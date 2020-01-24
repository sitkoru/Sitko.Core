using System;

namespace Sitko.Core.Queue.Nats
{
    public class NatsMessageOptions<T> : IQueueMessageOptions<T> where T : class, new()
    {
        public TimeSpan? StartAt { get; set; }
        public bool All { get; set; }
        public bool Durable { get; set; } = true;
        public int MaxInFlight = 200;
        public bool ManualAck = true;
        public int AckWait = (int)TimeSpan.FromSeconds(2).TotalMilliseconds;
    }
}
