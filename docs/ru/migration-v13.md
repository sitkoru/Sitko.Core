# Переход на Sitko.Core 13

[English](../en/migration-v13.md)

Версия 13 содержит несовместимое сокращение состава библиотек. Перечисленные
пакеты не выпускаются в составе 13; их типы и методы регистрации недоступны.
При обновлении проверьте прямые и транзитивные зависимости приложения,
уберите ссылки на эти пакеты, пространства имён и прежнюю конфигурацию.

## Экспорт OTEL

Для `HttpProtobuf` поле `OpenTelemetry:Endpoint` задаёт базовый URL. Если конфигурация
содержит точный адрес, например `http://collector:4318/v1/traces`, замените его на
`http://collector:4318`: модуль добавляет `/v1/logs`, `/v1/metrics`, `/v1/traces`
для соответствующих сигналов. Нестандартный приёмник с единым точным HTTP-путём
должен предоставлять эти пути либо маршрутизировать их у себя. gRPC адрес не меняется.

Непустые общие `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_PROTOCOL` и
`OTEL_EXPORTER_OTLP_HEADERS` имеют приоритет над соответствующими настройками
раздела для всех трёх сигналов. `OTEL_EXPORTER_OTLP_HEADERS` заменяет весь набор,
поэтому перечислите в нём все нужные заголовки, включая организацию. Убирать эти
переменные при переходе не требуется. Для хранения авторизации в Vault доступен
словарь `OpenTelemetry:Headers`; непустая стандартная env переменная продолжит его
переопределять. После изменения перезапустите приложение. Допустимые значения,
ограничения и точная поддержка env перечислены в [руководстве OTEL](observability.md).

## Платформа .NET 10

Пакеты Microsoft и интеграции .NET (Npgsql, Razor, MudBlazor, gRPC, валидация,
логирование и OpenTelemetry) используют актуальные совместимые стабильные версии.
AntDesign сохраняет версию `0.20.4`; Duende обновлён до `4.3.0` в модуле OIDC.
Для `OpenTelemetry.Instrumentation.GrpcNetClient` используется предварительная
версия `1.20.1-beta.1`: стабильного выпуска у этого пакета нет.

Пакеты Sitko.Core 13 содержат только сборки `net10.0`. Приложения на .NET 8
должны перейти на .NET 10 перед обновлением пакетов. Сборка репозитория использует
стабильный SDK из [global.json](../../global.json); для сборки приложения достаточно
совместимого SDK .NET 10. Для запуска установите актуальную среду выполнения
.NET 10, а для веб-приложений — ASP.NET Core 10; обновите базовые контейнеры
и среду сборки приложения. Поддержка .NET 11 в этот выпуск не входит.

Согласуйте прямые ссылки приложения на Microsoft/ASP.NET Core/EF Core с зависимостями
пакетов 13; не закрепляйте более старые версии транзитивных зависимостей.
Семейства Microsoft могут иметь разные номера версий. Настройки и правила
выбора версий описаны в [конфигурации сборки](build-configuration.md).

При переходе с варианта .NET 8 API `IEFRepository<TEntity>.UpdateAllAsync`
использует `Action<UpdateSettersBuilder<TEntity>>`. Если приложение передаёт
цепочку `SetProperty`, замените её блоком отдельных вызовов:

```csharp
await repository.UpdateAllAsync(item => item.IsActive, setters =>
{
    setters.SetProperty(item => item.Title, "Updated");
    setters.SetProperty(item => item.Count, item => item.Count + 1);
});
```

Это тот же API, который уже предоставлял вариант пакета для .NET 10.
Переход платформы не требует самостоятельного изменения схемы базы данных;
изменения модели приложения и миграции EF Core проверяйте отдельно.

## Ресурсы AntDesign

`Sitko.Core.Blazor.AntDesign` сохраняет компоненты, их API и зависимость
`AntDesign 0.20.4`. Ресурс `_content/Sitko.Core.Blazor.AntDesign/fileupload.js`
и дополнительные стили списков загрузки в v13 не поставляются.

Если приложение явно подключает этот скрипт, удалите подключение.
Компоненты используют базовое оформление AntDesign. Если приложению нужны
прежние дополнительные правила для размеров карточек, имён файлов или кнопок,
сопровождайте эти стили в самом приложении и проверьте формы загрузки.

## Клиент Redis и переход с v12

`Sitko.Core.Redis` использует `StackExchange.Redis 3.4.20`. Согласуйте прямые ссылки
приложения на клиент с этой версией. Формат подключения v13 несовместим с плоскими
параметрами v12: [единый контракт](redis-sentinel.md) находится в `Redis:Connection`.
Самостоятельный Redis, Auth Data Protection и Redis-хранилище токенов разделяют один
транспорт и одну базу в приложении. Модульных подключений и резервного чтения старых полей нет.

| Прежняя настройка или API | Действие для v13 |
| --- | --- |
| `Redis:Host/Port/Db/User/Password`, плоские `Auth.Redis*` | Задайте адреса, `user`, `password`, `defaultDatabase` в `Redis:Connection:Configuration`. Старые свойства удалены из `RedisModuleOptions` и `AuthOptions`. |
| `SentinelEndpoints`, `ServiceName`, `SentinelUser`, `SentinelPassword` и варианты `Auth.Redis*` | Выберите `Mode=Sentinel`, поместите `serviceName` и реквизиты данных в `Configuration`, адреса и реквизиты Sentinel — в `SentinelConfiguration`. |
| Keyed `RedisConnection` / `IDatabase` по типу AuthOptions | Получайте общий `RedisConnection` / `IDatabase` без ключа. Multiplexer доступен через `RedisConnection.Connection`; потребитель его не освобождает. |
| Переопределение `AuthOptionsValidator.RequiresRedis` | Условие регистрации находится в `AuthModule.RequiresRedis`; пользовательскому модулю при необходимости следует переопределить его. |

`EnableRedisDataProtection`, `DataProtectionLifeTimeInMinutes` и `TokenStoreType`
сохраняются. Auth автоматически подключает Redis только при необходимости.
Имена ключей, JSON токенов и назначение защиты не меняются.

В общем документе конфигурации v12/v13 оставьте старые значения и добавьте новый раздел.
Пример адресов условный:

```json
{
  "Redis": {
    "Host": "old-valkey",
    "Port": 6379,
    "Db": 0,
    "ConnectionString": "old-valkey:6379,defaultDatabase=0",
    "Connection": {
      "Mode": "Direct",
      "Configuration": "cluster-node-1:6379,cluster-node-2:6379,cluster-node-3:6379,defaultDatabase=0"
    }
  }
}
```

v12 продолжает читать прежние поля, игнорируя новый вложенный раздел; v13 читает
только новый. Сохраните также прежние `Auth.Redis*`, `RedisPersistent` и другие
поля, которые ещё читают приложения v12. Не заменяйте их адресом Cluster.

1. Задайте адреса нового Cluster в общем `Redis:Connection`, сохранив старые поля
   с адресами прежнего сервера для v12. Для Cluster используйте `Mode=Direct`,
   без `serviceName`; реквизиты и поддерживаемую сервером базу укажите в `Configuration`.
2. До обновления приложения перенесите необходимые ключи Data Protection в целевую
   базу, сохранив `ApplicationName`, окружение и claim пользователя. Формат ключей
   и защищённых данных сохраняется; библиотека сама данные не копирует.
3. Обновите приложение до v13. Оно начнёт использовать Cluster из нового раздела;
   приложения v12 продолжат обращаться к прежнему серверу. Отдельное переопределение
   адреса для каждого приложения не требуется. Проверьте чтение прежних защищённых
   данных, обычные операции и восстановление после смены основного узла.

В сценарии Sitkoru ключи Data Protection уже скопированы по подтверждению
сопровождающего; переход на Cluster выполняется обновлением версии. Фактическую
сохранность этой копии проверки библиотеки не подтверждают. Перенос кэшей, сессий
и токенов не является обязательным этапом этого сценария; при необходимости
сохранить такое состояние его перенос согласуется отдельно.

После новых записей в Cluster возврат на v12 не переносит актуальное состояние
обратно на прежний сервер. Ненулевые DB не объединяйте с DB0 без отдельного решения;
поддержка таких DB в Cluster зависит от сервера. Изменение Sitko.Core не переводит
собственные регистрации в MYCG.Core, Communications.Core и приложениях: их прямые
подключения проверяются отдельно. Настройки Vault и реальные данные библиотека не меняет.

## Поисковый клиент OpenSearch

`Sitko.Core.Search.OpenSearch` использует `OpenSearch.Client 2.3.0` и
`OpenSearch.Net 2.3.0`. По [матрице поставщика](https://github.com/opensearch-project/opensearch-net/blob/main/COMPATIBILITY.md) клиент 2.x соответствует серверу
OpenSearch 3.x. Если приложение напрямую использует типы или закрепляет версии
этих пакетов, согласуйте ссылки с версией 2.x и проверьте
[несовместимые изменения клиента](https://github.com/opensearch-project/opensearch-net/blob/main/UPGRADING.md).

Настройки модуля и публичные поисковые операции Sitko.Core сохраняются.
Клиент использует стандартный сериализатор Utf8Json; автоматического переключения
на System.Text.Json нет. Перед обновлением приложения проверьте создание и
повторную инициализацию индексов, запись, поиск и удаление документов на своей
версии сервера. Обновление клиентского пакета не обновляет сервер OpenSearch.

## Обновления зависимостей и совместимость

`Sitko.Core.ClickHouse.Linq2db` использует `linq2db 6.5.1` вместо `6.3.0`.
Публичный `BaseClickHouseDbContext` наследует `DataConnection`, поэтому изменения
перевода запросов затрагивают и запросы приложения. Перед переходом проверьте:

- Разность дат в `TotalDays`/`TotalHours` и других единицах вычисляет прошедшее время,
  а не число пересечённых календарных границ.
- Вычисления в проекции над числовыми полями и полями `bool`, не допускающими
  `null`, у отсутствующей строки `LEFT JOIN` учитывают значение .NET по умолчанию;
  фильтры и агрегаты сохраняют SQL `NULL`. Это изменение не относится к полям с преобразователями значений.
- Операции над наборами и столбцами с разными преобразователями значений могут
  завершаться `LinqToDBException` при несовместимом хранении. Для одинаковых
  преобразований через делегаты используйте выражения `HasConversion`.
- Аргументы `FormattableString`, сохранённой в переменной и использованной в запросе,
  передаются SQL-параметрами. Если база допускает в этой позиции только литерал,
  используйте `RawSqlString`.

Подробности и остальные изменения приведены в [примечаниях LINQ2DB 6.4–6.5.1](https://github.com/linq2db/linq2db/wiki/releases-and-roadmap#release-651).

`Storage` использует `MimeMapping 4.0.0`. Метод `MimeUtility.GetMimeMapping(string)`
сохранён, но справочник расширений и алгоритм выбора по приоритету обновлены:
для расширений с несколькими MIME-типами вычисленный тип может измениться.
Проверьте значимые для приложения расширения и зависящие от MIME-типа правила; см. [изменения справочника](https://github.com/zone117x/MimeMapping/releases/tag/v4.0.0).

`Grpc.Client` использует `OpenTelemetry.Instrumentation.GrpcNetClient 1.20.1-beta.1`
с ядром OpenTelemetry `1.20.1`. При переходе с `1.15.1-beta.1` учтите обновление
семантических соглашений до `1.42.0` и передачу контекста независимо от
`SuppressDownstreamInstrumentation`; это может изменить родителя серверного
сегмента трассировки. Проверьте запросы и правила мониторинга, использующие RPC-атрибуты
и связи сегментов, по [примечаниям инструментации](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/4f2a49444969d8ffae3d46007ce533d28ad153d3/src/OpenTelemetry.Instrumentation.GrpcNetClient/CHANGELOG.md).

## Удаление NATS/STAN

В версии 13 удалён пакет `Sitko.Core.Queue.Nats`: `AddNatsQueue`, `NatsQueue`,
`NatsQueueModule`, `NatsQueueModuleOptions`, `NatsMessageOptions<T>` и секция
`Queue:Nats` не поддерживаются. Зависимости `NATS.Client` и `STAN.Client`
исключены из состава Sitko.Core.

## Аннотации для инструментов разработки

Пакеты Sitko.Core не объявляют прямую зависимость от `JetBrains.Annotations`
и не используют его
атрибуты `PublicAPI` и `UsedImplicitly`. Если приложение получало этот пакет
транзитивно и использует его атрибуты, добавьте собственную прямую ссылку
на `JetBrains.Annotations` либо удалите ненужные аннотации приложения.

В графе Blazor этот пакет может сохраняться транзитивно через
`Sitko.Blazor.ScriptInjector`; удаление прямой ссылки Sitko.Core не исключает
зависимостей сторонних библиотек.

Это изменение подсказок инструментам разработки не меняет регистрацию модулей
или выполнение операций. Атрибуты nullable-анализа из
`System.Diagnostics.CodeAnalysis` сохраняются.

## Состав и действия при переходе

В таблице все имена модулей имеют префикс `Sitko.Core.`.

| Удалённые модули | Что меняется и что сделать в приложении |
| --- | --- |
| `Auth.Basic` | Удалите `AddBasicAuth()` и настройки `Auth:Basic`. Если Basic Auth нужен, подключите обработчик аутентификации напрямую и настройте допуск. |
| `Auth.Google` | Удалите `AddGoogleAuth()` и настройки `Auth:Google`. Подключите OIDC или обработчик Google напрямую; перенесите правила допуска пользователей. Успешный вход сам по себе не заменяет прежний список разрешённых пользователей. |
| `Automapper` | Удалите регистрацию модуля `AddAutoMapper()`. Зарегистрируйте AutoMapper напрямую с нужными сборками и профилями либо перенесите преобразования на Mapperly. Расширение сторонней библиотеки с тем же именем не является удалённым модулем Sitko.Core. |
| `ElasticStack`, `Queue.Apm` | Удалите `AddElasticStack()` и `AddQueueElasticApm()`, настройки Elastic APM и регистрацию его middleware очереди. Настройте нужный экспорт через OpenTelemetry или подключите интеграцию напрямую. |
| `Email.MailGun` | Удалите `AddMailGunEmail()` и настройки `Email:Mailgun`. Подключите отправитель Mailgun напрямую либо выберите сохраняемый `Email.Smtp`. |
| `HangFire` | Удалите `AddHangfirePostgres()` и использование типов этого модуля. Регистрацию Hangfire, хранилища и панели выполните напрямую, если они нужны. Сторонний `Hangfire.Core` остаётся зависимостью сохраняемого `Email`. |
| `Health`, `Health.Telegram` | Удалите `AddTelegramHealthReporter()`, настройки `Health:Telegram` и наследование `BaseHealthCheckPublisher`. При необходимости реализуйте уведомления через стандартный `IHealthCheckPublisher`. Конечные точки `/healthz`, `/ready`, `/health/all`, `/health/startup` остаются в `App.Web`. |
| `IdProvider`, `IdProvider.SonyFlake` | Удалите `AddSonyFlakeIdProvider()` и настройки `IdProvider:SonyFlake`. Если использованы `IIdProvider`, `FakeIdProvider`, `RandomGenerator` или `IdGenerationException`, замените их на подходящую приложению реализацию; встроенной замены генератора ID нет. |
| `MediatR` | Удалите регистрацию модуля `AddMediatR<TAssembly>()`. Для обычного MediatR подключите библиотеку напрямую. Передача уведомлений через `QueueModuleOptions.TranslateMediatRNotification<T>()`, `TranslateMediatRTypes` и требование `IMediatRModule` удалены; отправляйте сообщения через сохраняемый `IQueue` явно. |
| `OpenSearch` | Удалите `AddOpenSearchLogging()` и настройки `OpenSearch:Logging`; настройте экспорт логов через OTEL или прямое подключение нужного sink. Поисковый модуль `Search.OpenSearch` сохраняется. |
| `Puppeteer` | Удалите `AddPuppeteer()`, настройки `Puppeteer` и зависимости от `IBrowserProvider` этого модуля. Для собственного управления браузером подключите PuppeteerSharp напрямую. Для PDF достаточно `AddPdfRenderer()`; параметры и подготовка браузера описаны в [инструкции PDF](pdf.md#переход-на-sitkocore-13). |
| `Queue.Nats` | См. [удаление NATS/STAN](#удаление-natsstan). |
| `Search.ElasticSearch` | Удалите `AddElasticSearch()` и настройки `Search:Elastic`. Выберите прямую интеграцию Elasticsearch либо отдельно спланируйте переход на `Search.OpenSearch`; автоматической миграции индексов и настроек нет. |
| `Sentry` | Удалите `AddSentry()` и настройки модуля `Sentry`. При необходимости подключите SDK Sentry напрямую и восстановите необходимые параметры. |
| `Swagger` | Удалите регистрацию модуля `AddSwagger()` и настройки `Swagger`. Зарегистрируйте генерацию OpenAPI и интерфейс документации напрямую, включая описание авторизации и настройки конечных точек. |
| `Repository.Remote`, `Repository.Remote.Wasm`, `Repository.Remote.Server` | Удалите ссылки на три пакета, `AddRemoteRepositories()`, `AddHttpRepositoryTransport()`, `AddWasmHttpRepositoryTransport()` и настройки `Repositories:Remote`/`Repositories:Remote:Transports:Http`. Замените наследование от `BaseRemoteRepositoryController` и использование удалённых репозиториев явными клиентскими и серверными операциями выбранного API. Передача сериализованных LINQ-запросов по этому HTTP-транспорту не поддерживается; автоматического совместимого переноса на другой транспорт нет. |

## Сохраняемые возможности

`Blazor.FluentValidation`, `Identity`, `Queue.InMemory`, `Search.OpenSearch` и
`Pdf` входят в сохраняемый состав. Обычные публикация сообщений и обработчики
очереди работают без интеграции MediatR.

`Storage.Remote` и `Repository.Grpc` сохраняют самостоятельные контракты и
не относятся к удалённой тройке. Демонстрационный `apps/Blazor` сохраняет
формы, валидацию, навигацию и загрузку файлов без `Repository.Remote*`;
запуск и проверка описаны в [инструкции Blazor](blazor-demo.md).

Пакет `Auth.IdentityServer` заменён на `Auth.OIDC`. Регистрации, секции конфигурации,
настройка Keycloak по роли клиента и переход с Google описаны в
[инструкции OIDC/JWT](oidc-authentication.md).

## Проверка приложения

1. Уберите прямые ссылки и проверьте транзитивные ссылки общих библиотек.
2. Замените удалённые регистрации и параметры конфигурации выбранной интеграцией.
3. Пересоберите приложение и выполните его проверки входа, допуска, преобразований,
   доставки сообщений, логирования, PDF и документации API по используемым сценариям.
4. Проверьте работу в целевом окружении перед обновлением развёртывания.

Изменение состава Sitko.Core не выполняет переход приложений и окружений.
Контракт состава: [спецификация](../../specs/20261027-233345-legacy-modules/spec.md).

## Тестовая инфраструктура

Тестовые проекты используют xUnit4.0.2, MTP2.5.2 и AwesomeAssertions9.6.0. Ссылки на раннер, настройки SDK, пространства имён и команды описаны в [инструкции перехода оснастки](testing.md#переход-потребителя-оснастки).

## Клиент gRPC и включение обнаружения сервисов

Удалите ссылки на `Sitko.Core.Consul`, `Sitko.Core.ServiceDiscovery.Resolver.Consul`,
`Sitko.Core.ServiceDiscovery.Server` и `Sitko.Core.ServiceDiscovery.Server.Consul`.
Подключите `Sitko.Core.ServiceDiscovery.Consul` там, где требуется обнаружение.
`AddConsul()` и обобщённые `AddServiceDiscovery<TRegistrar, TResolver>()` /
`AddServiceDiscovery<TResolver>()` заменяются на `AddConsulServiceDiscovery()`.
Собственные типы прежнего resolver/registrar и `sd:///` удалены.

`Consul:ConsulUri` переносится в `ServiceDiscovery:Consul:Address`; DNS-адреса
задаются отдельно в `DnsServers`. Старые секундные параметры проверки/обновления
не считываются: настройте `Ttl`, `HeartbeatInterval`, `DnsRefreshPeriod` и
`DeregisterCriticalServiceAfter` по их назначению в новой схеме. Старые записи
приложения/порта новая схема не читает; взаимодействующие серверы и клиенты
обновляются согласованно.

`AddExternalGrpcClient<T>` заменяется на `AddGrpcClient<T>`. Прямой адрес задаётся
в `Grpc:Client:Contracts:<полное protobuf-имя>:Address`; без адреса клиент требует
подключённый провайдер обнаружения. Для Consul используйте
`AddConsulServiceDiscovery()` и секцию `ServiceDiscovery:Consul`.

Наследование от `GrpcClientModule<TClient, TOptions>` и `BaseGrpcServerModule<TOptions>`
не поддерживается. Клиент настраивается через `AddGrpcClient<TClient>(...)` и
`ConfigureGrpcClients(...)`, сервер — через `AddGrpcServer(...)` и регистрацию
сервисов. Действующие модули — `GrpcClientModule<TClient>` и `GrpcServerModule`.

Регистрации `options.RegisterService<T>()` и `options.RegisterServiceWithGrpcWeb<T>()`
перенесите из обработчика options в цепочку builder:

```csharp
app.AddGrpcServer(options => options.RequireSchemeAuthorization("Bearer"))
    .RegisterService<InvoiceGrpcService>()
    .RegisterServiceWithGrpcWeb<ReportsGrpcService>();
```

При SD выбор адреса выполняет стандартный `RoundRobin` Microsoft. Прежний
`LoadBalancingPolicy` не управляет этим путём; эквивалентность `None`/`PickFirst`
не сохраняется.

Общие клиентские параметры задаются через `ConfigureGrpcClients(...)`;
`Grpc:Client:Default` и `Grpc:Server:Default` содержат общую конфигурацию,
а `Contracts:<полное protobuf-имя>` — индивидуальные значения.

Удалите `Grpc:Server:EnableServiceDiscovery`. Общий `ServiceDiscovery:Enabled=false`
отключает обнаружение, публикацию и TTL. Сервер без подключения SD и клиенты
с прямыми адресами продолжают работать. Клиент без адреса при выключенном SD
завершает запуск с ошибкой конфигурации.

Идентичность клиента использует полное protobuf-имя, например
`accounting.v1.Invoice`, а не короткое C#-имя. Перенесите настройки контрактов
на полные имена. Примеры регистрации, конфигурация TLS и параметры провайдера
описаны в [инструкции gRPC/SD](grpc-service-discovery.md).

### Отдельный переход MYCG.Core

Обновление MYCG.Core выполняется отдельной задачей. Единый `AddMycgGrpc()` должен
задать через `ConfigureGrpcClients` поставщик `MYCG.Core.App.GrpcTokenProvider`,
общие настройки логирования и принятую в приложении политику сертификатов.
Зависимости поставщика токена регистрируются обычным DI с подходящим временем жизни.
Сам метод не должен неявно включать сервер или Consul.

После этого бизнес-клиенты и сервисы используют обычные регистрации Sitko.Core.
Обёртки выбора прямого адреса или SD, `GrpcClientsAddresses` и статический `IsGrpcConfigured`
заменяются конфигурацией контрактов и состоянием серверного модуля приложения.
Подключение SD из `AddMinimal`/`AddWeb` убирается. Принудительное включение reflection
в серверной обёртке заменяется явной настройкой приложения.

Входящая JWT/OIDC-авторизация, доменные protobuf-типы, преобразования типов и пользовательский
контекст MYCG остаются отдельными механизмами. Эта поставка Sitko.Core не меняет
код MYCG.Core и не подтверждает работу внешнего IdP.
