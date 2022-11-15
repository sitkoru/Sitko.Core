namespace Sitko.Core.Queue.Nats;

public class NatsQueueModule : QueueModule<NatsQueue, NatsQueueModuleOptions>
{
    public override string OptionsKey => "Queue:Nats";
}

public class NatsQueueModuleOptions : QueueModuleOptions
{
    public List<Uri> Servers { get; set; } = new();
    public string ClusterName { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public string? ConsumerGroupName { get; set; }
    public int ConnectionTimeoutInSeconds { get; set; } = 10;
    public bool Verbose { get; set; }

    public string? QueueNamePrefix { get; set; }

    public NatsQueueModuleOptions AddServer(string host, int port)
    {
        Servers.Add(new Uri($"nats://{host}:{port}"));
        return this;
    }
}

