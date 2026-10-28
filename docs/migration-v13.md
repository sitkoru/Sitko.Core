# Переход на Sitko.Core 13

[English](migration-v13.en.md)

Версия 13 содержит несовместимое сокращение состава библиотек. Перечисленные
пакеты не выпускаются в составе 13; их типы и методы регистрации недоступны.
При обновлении проверьте прямые и транзитивные зависимости приложения,
уберите ссылки на эти пакеты, пространства имён и прежнюю конфигурацию.

## Платформа .NET 10

Пакеты Microsoft и интеграции .NET (Npgsql, Razor, MudBlazor, gRPC, валидация,
логирование и OpenTelemetry) используют актуальные совместимые стабильные версии.
AntDesign сохраняет версию `0.20.4`; Duende обновлён до `4.3.0` в модуле OIDC. Существующая предварительная версия `OpenTelemetry.Instrumentation.GrpcNetClient`
сохраняется, поскольку у этого пакета нет стабильного выпуска.

Пакеты Sitko.Core 13 содержат только сборки `net10.0`. Приложения на .NET 8
должны перейти на .NET 10 перед обновлением пакетов. Сборка репозитория использует
стабильный SDK из [global.json](../global.json); для сборки приложения достаточно
совместимого SDK .NET 10. Для запуска установите актуальную среду выполнения
.NET 10, а для веб-приложений — ASP.NET Core 10; обновите базовые контейнеры
и среду сборки приложения. Поддержка .NET 11 в этот выпуск не входит.

Согласуйте прямые ссылки приложения на Microsoft/ASP.NET Core/EF Core с зависимостями
пакетов 13; не закрепляйте более старые версии транзитивных зависимостей.
Семейства Microsoft могут иметь разные номера версий. Настройки и правила
выбора версий описаны в [конфигурации сборки](ru/build-configuration.md).

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

## Клиент Redis

`Sitko.Core.Redis` использует `StackExchange.Redis 3.4.20`. Согласуйте прямые
ссылки приложения на этот клиент с зависимостями Sitko.Core. Параметры подключения
Sitko.Core и выбор прямого подключения, Sentinel или Cluster сохраняются.
Перед обновлением развёртывания проверьте используемый режим подключения,
восстановление после смены основного узла, чтение токенов и ключей Data Protection.

## Поисковый клиент OpenSearch

`Sitko.Core.Search.OpenSearch` использует `OpenSearch.Client 2.2.0` и
`OpenSearch.Net 2.2.0`. По [матрице поставщика](https://github.com/opensearch-project/opensearch-net/blob/main/COMPATIBILITY.md) клиент 2.x соответствует серверу
OpenSearch 3.x. Если приложение напрямую использует типы или закрепляет версии
этих пакетов, согласуйте ссылки с версией 2.x и проверьте
[несовместимые изменения клиента](https://github.com/opensearch-project/opensearch-net/blob/main/UPGRADING.md).

Настройки модуля и публичные поисковые операции Sitko.Core сохраняются.
Клиент использует стандартный сериализатор Utf8Json; автоматического переключения
на System.Text.Json нет. Перед обновлением приложения проверьте создание и
повторную инициализацию индексов, запись, поиск и удаление документов на своей
версии сервера. Обновление клиентского пакета не обновляет сервер OpenSearch.

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
запуск и проверка описаны в [инструкции Blazor](ru/blazor-demo.md).

Пакет `Auth.IdentityServer` заменён на `Auth.OIDC`. Регистрации, секции конфигурации,
настройка Keycloak по роли клиента и переход с Google описаны в
[инструкции OIDC/JWT](ru/oidc-authentication.md).

## Проверка приложения

1. Уберите прямые ссылки и проверьте транзитивные ссылки общих библиотек.
2. Замените удалённые регистрации и параметры конфигурации выбранной интеграцией.
3. Пересоберите приложение и выполните его проверки входа, допуска, преобразований,
   доставки сообщений, логирования, PDF и документации API по используемым сценариям.
4. Проверьте работу в целевом окружении перед обновлением развёртывания.

Изменение состава Sitko.Core не выполняет переход приложений и окружений.
Контракт состава: [спецификация](../specs/20261027-233345-legacy-modules/spec.md).

## Тестовая инфраструктура

Тестовые проекты используют xUnit4.0.2, MTP2.5.1 и AwesomeAssertions9.6.0. Ссылки на раннер, настройки SDK, пространства имён и команды описаны в [инструкции перехода оснастки](ru/testing.md#переход-потребителя-оснастки).

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
описаны в [инструкции gRPC/SD](ru/grpc-service-discovery.md).

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
