using System.Reflection;
using System.Text.Json.Serialization;
using Sitko.Core.App;

namespace Sitko.Core.Queue.Kafka;

public class KafkaQueueModuleOptions : BaseModuleOptions
{
    public string ClusterName { get; set; } = "Kafka_Queue";
    public string TopicPrefix { get; set; } = "";
    public string GroupPrefix { get; set; } = "";
    public bool StartConsumers { get; set; } = true;

    internal HashSet<Assembly> Assemblies { get; } = new();
    public int SimpleRetryCount { get; set; } = 5;

    [JsonIgnore]
    public Func<int, TimeSpan> SimpleRetryIntervals { get; set; } = i => i switch
    {
        1 => TimeSpan.FromSeconds(1),
        2 => TimeSpan.FromSeconds(30),
        _ => TimeSpan.FromMinutes(3)
    };

    public string ProducerName { get; set; } = "default";

    public string GetPrefixedTopicName(string topicName) => $"{TopicPrefix}-{topicName}";

    public string GetPrefixedGroupName(string groupName) => $"{GroupPrefix}-{groupName}";

    public KafkaQueueModuleOptions AddAssembly<T>()
    {
        Assemblies.Add(typeof(T).Assembly);
        return this;
    }
}
