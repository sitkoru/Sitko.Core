# Queues and background tasks

[Русский](../ru/messaging.md) · [All topics](README.md)

## IQueue: in-process and NATS

`Sitko.Core.Queue` defines `IQueue`, processors and middleware.
After [registering Sitko.Core](application.md), select a transport:
`AddInMemoryQueue(...)` from `Sitko.Core.Queue.InMemory` or `AddNatsQueue(...)`
from `Sitko.Core.Queue.Nats`.

For an in-process queue without an external server:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Queue;
using Sitko.Core.Queue.InMemory;

var builder = Host.CreateApplicationBuilder(args);
builder.AddSitkoCore().AddInMemoryQueue();
using var host = builder.Build();
await host.StartAsync();
var queue = host.Services.GetRequiredService<IQueue>();
var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var subscription = await queue.SubscribeAsync<Notice>((message, context) =>
{
    Console.WriteLine(message.Text);
    received.TrySetResult();
    return Task.FromResult(true);
});
await queue.PublishAsync(new Notice("Hello"));
await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
await host.StopAsync();

public record Notice(string Text);
```

Expected output: `Hello`. The example explicitly waits up to five seconds for reception:
publishing and callback completion are separate events. Keep the application Host
alive until the required work finishes. InMemory does not persist messages across
restarts or connect separate processes.

Sections are `Queue:InMemory` and `Queue:Nats`. Options expose
`RegisterProcessor<TProcessor,TMessage>()`, `RegisterProcessors<TAssembly>()`,
`RegisterMiddleware<T>()` and `ConfigureMessage<T>(...)`. Processors have scoped
lifetimes. Request/reply (`RequestAsync`/`ReplyAsync`) and explicit unsubscribe
(`UnsubscribeAsync`) are available. Check publication/subscription results and
missing replies; transport acceptance does not prove business operation completion.

NATS uses **STAN** and requires NATS Streaming, not just NATS Core or JetStream.
Set `Servers`, `ClusterName` and optionally `ClientName`, `ConsumerGroupName`,
`QueueNamePrefix`. The connection timeout is 10 seconds.
`NatsMessageOptions<T>` defaults to `Durable=true`, `ManualAck=true`,
`AckWaitInSeconds=2`, `MaxInFlight=200`. With manual acknowledgement, a callback
returning `false` does not acknowledge the message; redelivery is possible.
Make application actions safe to repeat.

## Kafka

`Sitko.Core.Kafka` provides KafkaFlow infrastructure under section `Kafka`.
Set `Brokers`, security parameters and suitable topic settings.
`TopicPartitionsCount=24`, `TopicReplicationFactor=1`, `AutoOffsetReset=Latest`;
these defaults are not a production cluster reliability policy.

`Sitko.Core.Queue.Kafka` uses `AddKafkaQueue(options => options.AddAssembly<TAssembly>())`.
It adds the common Kafka module if not already registered. Its API is separate:
`IEventProducer.Produce(key, message)`, `QueueEventAttribute`, `QueueHandlerAttribute`
and `QueueBatchHandlerAttribute`; it is not an `IQueue` transport.
Section `Kafka:Queue` contains `TopicPrefix`, `GroupPrefix`, `StartConsumers=true`
and `SimpleRetryCount=5`. Current name construction inserts a hyphen between
prefix and name even when the prefix is empty; align actual names with other parties.

Only `Simple` connects retry handling in this module; `Forever`, `DurableOrdered`
and `DurableLatest` do not implement those policies. The API does not guarantee
exactly-once execution or automatically protect application side effects.
See [Kafka tests](../../tests/Sitko.Core.Queue.Kafka.Tests) for registrations and handlers.

## Stateful tasks

`Sitko.Core.Tasks` defines task state models, executors and maintenance;
`Sitko.Core.Tasks.Kafka` dispatches through Kafka and persists tasks through EF.
`AddKafkaTasks<TBaseTask,TDbContext>(...)` needs application models and a subclass of
`TasksDbContext<TBaseTask>`. Configure `TasksTopic`,
`AddExecutorsFromAssemblyOf<TAssembly>()`, then `AddTask<...>(cronExpression)`
for schedules. `configurePostgres=false` by default: register the context yourself
or explicitly enable PostgreSQL configuration. `configureKafka=true` adds the
common module only if absent.

The section is `Kafka:Tasks:<base task type name>`. Shared settings include
`IsAllTasksDisabled`, `DisabledTasks`, `AllTasksRetentionDays`, `RetentionDays`,
`TasksInactivityTimeout` (30 minutes), `TasksWaitTimeout` (60 minutes),
`StuckTasksProcessMode=Fail`, `UseDistributedLock=true`, `RetryDelayInSeconds=60`.
Default topic/group prefixes include the application and environment.

Verify delivery, successful/failed completion, retry and persisted state in an
isolated environment. For a real Kafka/PostgreSQL example, see
[Tasks.Kafka.Tests](../../tests/Sitko.Core.Tasks.Kafka.Tests).
Sources: [IQueue](../../src/Sitko.Core.Queue/IQueue.cs),
[NATS](../../src/Sitko.Core.Queue.Nats/NatsQueue.cs),
[Kafka](../../src/Sitko.Core.Queue.Kafka/KafkaQueueModule.cs),
[tasks](../../src/Sitko.Core.Tasks.Kafka/ApplicationExtensions.cs).
