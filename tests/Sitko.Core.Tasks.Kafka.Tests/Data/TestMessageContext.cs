using KafkaFlow;

namespace Sitko.Core.Tasks.Kafka.Tests.Data
{
    public sealed class TestMessageContext : IMessageContext, IConsumerContext
    {
        public IConsumerContext ConsumerContext => this;
        public bool ShouldStoreOffset { get; set; } = true;
        public bool AutoMessageCompletion { get; set; } = true;
        public Message Message => throw new NotSupportedException();
        public IMessageHeaders Headers => throw new NotSupportedException();
        public IProducerContext ProducerContext => throw new NotSupportedException();
        public IDictionary<string, object> Items => throw new NotSupportedException();
        public IDependencyResolver DependencyResolver => throw new NotSupportedException();
        public IReadOnlyCollection<string> Brokers => throw new NotSupportedException();
        public string ConsumerName => throw new NotSupportedException();
        public CancellationToken WorkerStopped => throw new NotSupportedException();
        public int WorkerId => throw new NotSupportedException();
        public string Topic => throw new NotSupportedException();
        public int Partition => throw new NotSupportedException();
        public long Offset => throw new NotSupportedException();
        public TopicPartitionOffset TopicPartitionOffset => throw new NotSupportedException();
        public string GroupId => throw new NotSupportedException();
        public DateTime MessageTimestamp => throw new NotSupportedException();
        public IDependencyResolver ConsumerDependencyResolver => throw new NotSupportedException();
        public IDependencyResolver WorkerDependencyResolver => throw new NotSupportedException();
        public Task<TopicPartitionOffset> Completion => throw new NotSupportedException();
        public IMessageContext SetMessage(object key, object value) => throw new NotSupportedException();
        public void Complete() => throw new NotSupportedException();
        public IOffsetsWatermark GetOffsetsWatermark() => throw new NotSupportedException();
        public void Pause() => throw new NotSupportedException();
        public void Resume() => throw new NotSupportedException();
        public void Pause(IReadOnlyList<TopicPartition> topicPartitions) => throw new NotSupportedException();
        public void Resume(IReadOnlyList<TopicPartition> topicPartitions) => throw new NotSupportedException();
    }
}
