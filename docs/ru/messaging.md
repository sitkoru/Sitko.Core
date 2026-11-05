# Очереди и фоновые задачи

[English](../en/messaging.md) · [Все темы](README.md)

## IQueue: внутри процесса

`Sitko.Core.Queue` задаёт `IQueue`, процессоры и промежуточные обработчики.
После [подключения Sitko.Core](application.md) зарегистрируйте очередь
через `AddInMemoryQueue(...)` из `Sitko.Core.Queue.InMemory`.

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

Секция — `Queue:InMemory`. В options доступны
`RegisterProcessor<TProcessor,TMessage>()`, `RegisterProcessors<TAssembly>()`,
`RegisterMiddleware<T>()`, `ConfigureMessage<T>(...)`. Процессоры регистрируются
со scoped-временем жизни. Есть запрос/ответ (`RequestAsync`/`ReplyAsync`)
и явная отписка (`UnsubscribeAsync`). Проверяйте результат публикации/подписки
и отсутствие ответа; принятие транспорта не доказывает выполнение бизнес-операции.

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

Для обработчиков потребителя только стратегия `Simple` подключает повторные попытки;
значения `Forever`, `DurableOrdered`, `DurableLatest` не реализуют такую политику.
Ожидания ровно одного выполнения или автоматической защиты побочных эффектов
API не даёт. Примеры регистрации и обработчиков:
[Kafka-тесты](../../tests/Sitko.Core.Queue.Kafka.Tests).

### Подтверждение отправки

Если приложению нужен результат доставки, используйте
`await producer.ProduceAsync(key, message)`. Метод возвращает
`Confluent.Kafka.DeliveryResult<byte[], byte[]>` со статусом `Persisted`,
непустым `Topic`, неотрицательными `Partition` и `Offset`. Это подтверждение
записи брокером, а не выполнения обработчика потребителя.

```csharp
var report = await producer.ProduceAsync(key, message);
Console.WriteLine($"{report.Topic}/{report.Partition.Value}/{report.Offset.Value}");
```

`Produce(key, message)` отправляет без возврата отчёта вызывающему коду.
Оба метода используют одну регистрацию, маршрутизацию и сериализацию событий.
Собственные реализации `IEventProducer` должны реализовать `ProduceAsync`.

`ProduceAsync` сохраняет исходные исключения KafkaFlow. В
`ProduceException<byte[], byte[]>` смотрите `DeliveryResult.Status`:
`PossiblyPersisted` означает, что запись могла состояться. `NotPersisted` сам по себе
не доказывает недоставку: Confluent.Kafka 2.8.0 пересоздаёт отчёт некоторых ошибок
в `ProduceAsync`, теряя исходный статус доставки. Поэтому даже таймаут с
`NotPersisted` нельзя безопасно повторять. Другая ошибка без
надёжного отчёта не доказывает недоставку: исключение может возникнуть после записи.
Пустой отчёт, неподтверждённый статус или недействительные координаты вызывают
`InvalidOperationException`; такой исход также нельзя считать доказанной недоставкой.

Без явной регистрации политики дополнительных повторных отправок нет. Внутренние повторы Kafka
сохраняются: по умолчанию `MessageTimeout=12s`, `EnableIdempotence=true`, `Acks=All`.
Повторный вызов приложением является новой отправкой и при неизвестном исходе
может создать дубликат. Идемпотентность producer не устраняет этот риск.

Метод не принимает `CancellationToken`. Если прекратить ожидание через `WaitAsync`,
исходная отправка продолжится; сохранённую задачу можно дождаться отдельно.
Отмена или тайм-аут ожидания не означают отсутствия записи. `MessageTimeout`
ограничивает доставку средствами Kafka, но не всё выполнение пользовательских
промежуточных и завершающих обработчиков. Метод не сохраняет неотправленные
сообщения между перезапусками процесса.

### Дополнительные безопасные повторы

Приложение может повторять отправку, если локальная очередь producer переполнена
и сообщение ещё не принято в неё. Зарегистрируйте общую политику или политику
для конкретного типа события до построения Host:

```csharp
using Sitko.Core.Queue.Kafka;
using Polly;

builder.Services.AddKafkaProducerResiliencePipeline(options =>
{
    options.MaxRetryAttempts = 2;
    options.Delay = TimeSpan.FromMilliseconds(100);
    options.BackoffType = DelayBackoffType.Exponential;
});

// Пример: Notice — зарегистрированный тип события приложения.
builder.Services.AddKafkaProducerResiliencePipeline<Notice>(options =>
{
    options.MaxRetryAttempts = 4;
    options.Delay = TimeSpan.FromMilliseconds(200);
});

// Пример: CriticalNotice — другой зарегистрированный тип события.
builder.Services.DisableKafkaProducerResiliencePipeline<CriticalNotice>();
```

Политика типа либо его явное отключение имеют приоритет над общей настройкой;
две политики не складываются. Тип выбирается по `T` в `ProduceAsync<T>`, как и
маршрут события, а не по фактическому типу объекта. При повторной регистрации
одной области действует последняя. Без общей и типовой настройки отправка одна.
`Produce` и `Tasks.Kafka` эти настройки не используют.

Параметры — стандартный `Polly.Retry.RetryStrategyOptions`; внутри регистрируются
именованные pipeline через `Microsoft.Extensions.Resilience`. Имена выбирает
библиотека. Произвольную pipeline, timeout или hedging подключить нельзя.
`MaxRetryAttempts` — число дополнительных попыток, поэтому значение `2`
разрешает не более трёх отправок. Настраиваются стандартные задержки, jitter,
`DelayGenerator`, `OnRetry` и дополнительный `ShouldHandle`.

Обязательный барьер допускает только нефатальный `Local_QueueFull` в
`ProduceException<byte[], byte[]>` с `NotPersisted`, тем же topic, `Offset.Unset`
и partition `Any` либо неотрицательным. `ShouldHandle` приложения может сузить
этот набор, но не расширить его. Успех, отсутствующий или некорректный отчёт,
неизвестный исход, таймаут, ошибка сериализации, прав, размера или конфигурации
повтор не разрешают. При исчерпании попыток сохраняются последнее исходное
исключение и его `DeliveryResult`.

Попытки последовательны и используют прежние ключ, объект события и маршрут.
Учитывайте сумму внутренних ожиданий Kafka и дополнительных задержек:
новая попытка снова получает внутренний бюджет доставки Kafka. Порядок между
параллельными вызовами, ровно однократная обработка и сохранность после падения
процесса не гарантируются.

Основания ограничения: [пересоздание отчёта Confluent 2.8.0](https://github.com/confluentinc/confluent-kafka-dotnet/blob/v2.8.0/src/Confluent.Kafka/Producer.cs#L833)
и [отказ локальной очереди до принятия сообщения](https://github.com/confluentinc/librdkafka/blob/v2.8.0/src/rdkafka_int.h#L772).
Реализация: [регистрация политик](../../src/Sitko.Core.Queue.Kafka/KafkaProducerResilienceExtensions.cs).

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
[Kafka](../../src/Sitko.Core.Queue.Kafka/KafkaQueueModule.cs),
[задачи](../../src/Sitko.Core.Tasks.Kafka/ApplicationExtensions.cs).
