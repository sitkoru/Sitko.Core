# Очереди и фоновые задачи

[English](../en/messaging.md) · [Все темы](README.md)

## IQueue: внутри процесса и NATS

`Sitko.Core.Queue` задаёт `IQueue`, процессоры и промежуточные обработчики.
После [подключения Sitko.Core](application.md) выберите транспорт:
`AddInMemoryQueue(...)` из `Sitko.Core.Queue.InMemory` либо `AddNatsQueue(...)`
из `Sitko.Core.Queue.Nats`.

Для локальной очереди без внешнего сервера:

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

Ожидается `Hello`. Пример явно ждёт получения до пяти секунд: публикация
и завершение callback — разные события. В приложении удерживайте Host до
завершения нужной работы. InMemory не сохраняет сообщения между перезапусками
и не соединяет несколько процессов.

Секции — `Queue:InMemory` и `Queue:Nats`. В options доступны
`RegisterProcessor<TProcessor,TMessage>()`, `RegisterProcessors<TAssembly>()`,
`RegisterMiddleware<T>()`, `ConfigureMessage<T>(...)`. Процессоры регистрируются
со scoped-временем жизни. Есть запрос/ответ (`RequestAsync`/`ReplyAsync`)
и явная отписка (`UnsubscribeAsync`). Проверяйте результат публикации/подписки
и отсутствие ответа; принятие транспорта не доказывает выполнение бизнес-операции.

NATS использует **STAN**, поэтому нужен сервер с NATS Streaming, не просто NATS
Core или JetStream. Укажите `Servers`, `ClusterName`, при необходимости
`ClientName`, `ConsumerGroupName`, `QueueNamePrefix`.
Тайм-аут подключения — 10 секунд. `NatsMessageOptions<T>` по умолчанию включает
`Durable=true`, `ManualAck=true`, `AckWaitInSeconds=2`, `MaxInFlight=200`.
При ручном подтверждении callback с `false` не подтверждает сообщение;
повторная доставка возможна. Делайте прикладные действия устойчивыми к повторам.

## Kafka

`Sitko.Core.Kafka` содержит инфраструктуру KafkaFlow, секция `Kafka`.
Задайте `Brokers`, параметры безопасности и допустимые настройки topics.
`TopicPartitionsCount=24`, `TopicReplicationFactor=1`, `AutoOffsetReset=Latest`;
эти значения не являются готовой политикой надёжности рабочего кластера.

`Sitko.Core.Queue.Kafka` подключается через
`AddKafkaQueue(options => options.AddAssembly<TAssembly>())`. Он добавляет общий
Kafka-модуль, если тот ещё не зарегистрирован. Это отдельный API:
`IEventProducer.Produce(key, message)`, `QueueEventAttribute`, `QueueHandlerAttribute`
и `QueueBatchHandlerAttribute`; он не является транспортом `IQueue`.
Секция `Kafka:Queue` содержит `TopicPrefix`, `GroupPrefix`, `StartConsumers=true`
и `SimpleRetryCount=5`. Текущее построение имени добавляет дефис между префиксом
и именем даже при пустом префиксе; согласуйте реальные имена с другими участниками.

Только стратегия `Simple` подключает повторные попытки в этом модуле;
значения `Forever`, `DurableOrdered`, `DurableLatest` не реализуют такую политику.
Ожидания ровно одного выполнения или автоматической защиты побочных эффектов
API не даёт. Примеры регистрации и обработчиков:
[Kafka-тесты](../../tests/Sitko.Core.Queue.Kafka.Tests).

## Задачи с состоянием

`Sitko.Core.Tasks` задаёт модели состояния, исполнителей и обслуживание;
`Sitko.Core.Tasks.Kafka` передаёт выполнение через Kafka и хранит задачи в EF.
`AddKafkaTasks<TBaseTask,TDbContext>(...)` требует свои модели и наследника
`TasksDbContext<TBaseTask>`. В callback задайте `TasksTopic`,
`AddExecutorsFromAssemblyOf<TAssembly>()`, затем `AddTask<...>(cronExpression)`
для нужных расписаний. `configurePostgres=false` по умолчанию: зарегистрируйте
контекст сами либо явно включите настройку PostgreSQL. `configureKafka=true`
добавляет общий модуль только при его отсутствии.

Секция — `Kafka:Tasks:<имя базового типа задачи>`. Общие параметры включают
`IsAllTasksDisabled`, `DisabledTasks`, `AllTasksRetentionDays`, `RetentionDays`,
`TasksInactivityTimeout` (30 минут), `TasksWaitTimeout` (60 минут),
`StuckTasksProcessMode=Fail`, `UseDistributedLock=true`, `RetryDelayInSeconds=60`.
Префиксы topic/group по умолчанию включают приложение и окружение.

Проверяйте доставку, успешное и ошибочное завершение, повтор и сохранённое состояние
в собственном изолированном стенде. Образец с настоящими Kafka/PostgreSQL:
[Tasks.Kafka.Tests](../../tests/Sitko.Core.Tasks.Kafka.Tests).
Источники: [IQueue](../../src/Sitko.Core.Queue/IQueue.cs),
[NATS](../../src/Sitko.Core.Queue.Nats/NatsQueue.cs),
[Kafka](../../src/Sitko.Core.Queue.Kafka/KafkaQueueModule.cs),
[задачи](../../src/Sitko.Core.Tasks.Kafka/ApplicationExtensions.cs).
