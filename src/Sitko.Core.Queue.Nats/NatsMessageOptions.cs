using JetBrains.Annotations;

namespace Sitko.Core.Queue.Nats;

[PublicAPI]
public class NatsMessageOptions<T> : IQueueMessageOptions<T> where T : class
{
    public TimeSpan? StartAt { get; set; }
    public bool All { get; set; }
    public bool Durable { get; set; } = true;
    public int MaxInFlight { get; set; } = 200;
    public bool ManualAck { get; set; } = true;
    public int AckWaitInSeconds { get; set; } = 2;
}

