using KafkaFlow.Producers;
using Sitko.Core.Tasks.Data.Entities;
using Sitko.Core.Tasks.Scheduling;

namespace Sitko.Core.Tasks.Kafka.Scheduling;

public class KafkaTaskScheduler : ITaskScheduler
{
    private readonly IProducerAccessor producerAccessor;

    public KafkaTaskScheduler(IProducerAccessor producerAccessor) => this.producerAccessor = producerAccessor;

    public async Task ScheduleAsync(IBaseTask task) =>
        await producerAccessor.GetProducer(EventsRegistry.GetProducerName(task.GetType())).ProduceAsync(task.GetKey(), task);
}
