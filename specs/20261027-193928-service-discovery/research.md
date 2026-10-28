# Исследование: обнаружение gRPC-сервисов

Первичное исследование: 2026-10-27, Sitko.Core v12 `fa94b44322448f85c92956f5a88c14624cc2daf1`. Синхронизация принятых решений: 2026-10-28. Подготовка первоначально опиралась на `5e12c6756720d29bc3fc836b6735fcc7c03739e7`. Перед реализацией ветка перебазирована на v13 `b532204c103d1ed957889a2303ba6e9f8cabcc3f`. Исходные ссылки v12 сохранены как основания сравнения, а не описание обновлённой платформы. Архитектура и V01–V16 согласованы; реализация начата, V01/V04/V05 локально подтверждены. Текущее состояние задач — в tasks.md.

## Штатный Microsoft HTTP + DNS SRV

**Решение**: `Microsoft.Extensions.ServiceDiscovery` и `.Dns` 10.11.0, `AddDnsSrvServiceEndpointProvider`, `AddServiceDiscovery()` на существующем gRPC client builder.

**Основание**: пользователь выбрал минимальный штатный путь и допускает ошибки RPC при отказе Consul. `IHttpClientBuilder` уже является точкой регистрации клиента. Обработчик выбирает адрес и изменяет URI, затем использует HTTP-транспорт, не добавляя сетевой прокси.

**Альтернативы**: собственный Consul Health API provider отклонён на первом этапе; собственный мост к grpc-dotnet сохранял бы gRPC subchannels, но добавляет сопровождение и не нужен под принятое поведение.

[Microsoft SD](https://learn.microsoft.com/en-us/dotnet/core/extensions/service-discovery), [регистрация HTTP-обработчика](https://github.com/dotnet/extensions/blob/25f07afa51034e37b334210642d7dc5f191c142d/src/Libraries/Microsoft.Extensions.ServiceDiscovery/ServiceDiscoveryHttpClientBuilderExtensions.cs), [сам обработчик](https://github.com/dotnet/extensions/blob/25f07afa51034e37b334210642d7dc5f191c142d/src/Libraries/Microsoft.Extensions.ServiceDiscovery/Http/ResolvingHttpDelegatingHandler.cs).

Метаданные обоих установленных NuGet-пакетов 10.11.0 указывают на commit `25f07afa51034e37b334210642d7dc5f191c142d`. Обработчик специально отключает балансировку grpc-dotnet, поэтому прежний `sd:///` и `RoundRobinConfig` не остаются владельцами маршрутизации.

## Обновление и ошибки

**Решение**: поведение штатного DNS-провайдера сохраняется; нужные тайм-ауты и интервалы доступны в конфигурации, применяются после перезапуска.

**Основание**: после исчерпанных timeout/SERVFAIL DNS resolver возвращает пустой массив; SRV provider заменяет прежний набор. Пользователь осознанно принял этот компромисс. Дополнительный кэш последних адресов не вводится.

`DnsResolverOptions` содержит `Servers`, `Timeout`, `MaxAttempts` и читается через `IOptions` при создании. SRV provider читает интервалы через `IOptionsMonitor`: `DefaultRefreshPeriod`, `MinRetryPeriod`, `MaxRetryPeriod`, `RetryBackOffFactor`. Для Consul задаётся общий `ServiceDomainNameCallback`, формирующий `<имя>.service.consul`. TTL=0 не означает мгновенное обновление: применяется период провайдера. Произвольный fallback к обычному разрешению логического имени не нужен.

[DNS resolver](https://github.com/dotnet/extensions/blob/25f07afa51034e37b334210642d7dc5f191c142d/src/Libraries/Microsoft.Extensions.ServiceDiscovery.Dns/Resolver/DnsResolver.cs), [SRV provider](https://github.com/dotnet/extensions/blob/25f07afa51034e37b334210642d7dc5f191c142d/src/Libraries/Microsoft.Extensions.ServiceDiscovery.Dns/DnsSrvServiceEndpointProvider.cs), [опции DNS](https://github.com/dotnet/extensions/blob/25f07afa51034e37b334210642d7dc5f191c142d/src/Libraries/Microsoft.Extensions.ServiceDiscovery.Dns/DnsResolverOptions.cs).

## TLS, токены и полезная нагрузка

**Решение**: HTTPS остаётся в логическом адресе клиента, сохраняются `AddCallCredentials`, metadata providers и настройка проверки сертификата.

**Основание**: в `GrpcClientModuleOptions.ConfigureClient` токены и метаданные передаются через `AddCallCredentials`. По умолчанию grpc-dotnet применяет их только при TLS. Небезопасное исключение не включается. DNS SRV сообщает host/port, но не задаёт TLS identity; настройка сертификатов при миграции не переделывается.

[Авторизация gRPC](https://learn.microsoft.com/en-us/aspnet/core/grpc/authn-and-authz?view=aspnetcore-10.0), [ограничения повторов](https://learn.microsoft.com/en-us/aspnet/core/grpc/retries?view=aspnetcore-10.0).

На первичной основе v12 был grpc-dotnet 2.80.0. На принятой основе v13 уже используются runtime `2.84.0`, Tools `2.85.0`, Google.Protobuf `3.36.2`; откат не требуется. Исторические прототипы не доказывают TLS/токены и готовность новой реализации Sitko.Core. Проверка ответа 256 МиБ и сравнение времени/памяти удалены из приёмки по решению пользователя; V07 с 2 МиБ проверяет настройки лимитов.

## Идентичность и регистрация

**Решение**: полное protobuf-имя, отдельная регистрация контракта/экземпляра, окружение в DNS-имени; смешанный переход не поддерживается.

**Основание**: `GrpcServicesHelper` сейчас использует `DeclaringType.Name`, что смешивает короткие имена разных protobuf-пакетов. Старый `ServiceDiscoveryManager` публикует `приложение_порт` и собственные метаданные, а SRV-клиенту нужна регистрация самого логического контракта. Простая замена клиентского resolver оставила бы FR-002/003 невыполненными.

В исследованном MYCG.Core `4da7e4ae58972f0d1bef7d4f2960d6c08faf0611` обёртка `CGApplicationExtensions.AddGrpcClient` использует короткое имя из `GetServiceNameForClient<TClient>()` как ключ `GrpcClientsAddresses`. Это подтверждённый источник несовместимости, а не требование сохранить словарь. Принят согласованный переход: единый Sitko.Core `AddGrpcClient`, прямые адреса в `Contracts` по полному protobuf-имени и один `AddMycgGrpc` для политики. Обёртки, словарь и статические флаги удаляются при отдельном обновлении MYCG; его код в этой задаче не меняется. Новый SD не обязан сохранять старые helper ради отвергнутой схемы.

Для DNS используется общая ограниченная ASCII-форма с устойчивым хеш-суффиксом от точной пары окружение/контракт; исходные строки остаются в метаданных. Только замена разделителей и регистра недостаточна: она может смешать разные полные имена. Дополнительное хранилище соответствий не требуется.

[Определение сервиса Consul](https://developer.hashicorp.com/consul/docs/reference/service), [DNS Consul](https://developer.hashicorp.com/consul/docs/reference/dns).

## Фактический именованный порт

**Решение**: сохранить объект `ListenOptions` в существующем `AppWebConfigurationModule` и читать `IPEndPoint.Port` после готовности сервера. Для неизменённых внешних Kestrel-конфигураций допустим только однозначный подходящий адрес; неоднозначность требует явной настройки, а не выбора первого адреса.

**Основание**: текущий registrar сопоставляет настроенный порт `0` с уже фактическим Uri.Port и не находит его. `IServerAddressesFeature` не сохраняет имя настроенного порта. Модуль уже доступен через `IApplicationContext.GetModuleInstance<T>()`; отдельный универсальный реестр избыточен.

В Kestrel результат привязки записывается обратно в `ListenOptions.EndPoint`; `AnyIPListenOptions` сохраняет объект при IPv6/IPv4 fallback. `IHostedLifecycleService.StartedAsync` либо эквивалентное ожидание `ApplicationStarted` позволяют начать регистрацию после привязки. Один управляемый фоновый цикл владеет регистрацией/heartbeat/удалением.

[ListenOptions](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Servers/Kestrel/Core/src/ListenOptions.cs), [KestrelServerImpl](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Servers/Kestrel/Core/src/Internal/KestrelServerImpl.cs).

## Агент Consul и локальная сеть

Регистрация и TTL принадлежат одному агенту Consul; балансировщик между произвольными агентами не гарантирует их совместимость. После утраты регистрации допускается идемпотентная повторная регистрация у настроенного агента. Инфраструктурное развёртывание вне задачи.

Для клиентов и серверов на одном хосте TTL не требует входящего подключения из контейнера Consul, поэтому публикуется loopback. SRV Additional A/AAAA преобразуются штатным провайдером в IP endpoints. Для клиентов на другой машине или внутри контейнера loopback не подходит; такой локальный сценарий не заказан.

[Consul Agent Services](https://developer.hashicorp.com/consul/api-docs/agent/service), [Consul Agent Checks](https://developer.hashicorp.com/consul/api-docs/agent/check).

## Проверки и непроверенные свойства

Текущий SD-тест обращается к `localhost:8500`, использует фиксированный диапазон портов и проверяет поздний старт сервера. Согласованы изолированные реальные проверки V01–V16; их описание находится в [контракте проверок](contracts/verification.md). В репозитории уже есть Testcontainers 4.6.0 и генерация gRPC `.proto`; новая система проверки не нужна.

На этапе исследования не запускались тесты, приложения или контейнеры. Windows/Linux, производственная конфигурация и реальное восстановление новой реализации TLS-клиента Sitko.Core ещё не проверены. Проверка производительности в согласованный набор не входит.

## Принятые границы модулей и публичного API

**Решение**: общий SD остаётся отдельным и необязательным, его основание — Microsoft SD. `Resolver.Consul`, `Server.Consul` и нужный Consul-клиент объединяются в `Sitko.Core.ServiceDiscovery.Consul`; отдельный `Sitko.Core.Consul` удаляется. gRPC не зависит от Consul. Один выключатель `ServiceDiscovery.Enabled` управляет всем SD.

**Основание**: текущих потребителей самостоятельного Consul за пределами SD в исследовании не найдено. Сохранение слоя только ради прежней структуры не даёт нового поведения и распределяет настройки/включение. Перенос SD в App сделал бы инфраструктуру обязательной для не нуждающихся в ней приложений. Новый каталог подключений и фиктивный альтернативный провайдер не нужны.

**Альтернатива**: заменить только resolver, сохранив пакеты и обёртки, недостаточно для согласованных единого клиента, общего выключателя и уменьшения модулей. Обоснование по конкретным существующим точкам — [Constitution Check](plan.md#constitution-check).

Один клиент выбирает прямой `Address` либо SD; `Default`/`Contracts` и callbacks применяются по пятиуровневому приоритету. Сервер использует fluent-регистрацию, повторное добавление без дубликатов и итоговые общие настройки; reflection по умолчанию выключен. Общая политика токенов охватывает регистрацию до/после неё, токен читается для каждого RPC. Входящая JWT-авторизация и доменные protobuf/converters/context MYCG остаются отдельными механизмами. Полный целевой контракт — [module-api.md](contracts/module-api.md).

Контрактный TTL сохраняется. Регистрация всех приложений и Consul leader healthcheck не возвращаются; в проверенном v12 безусловной регистрации всех приложений уже нет. Исходный registrar ограничен объявленными SD-сервисами.

## Свежая платформа и ограничения подготовки

На основе v13 только `net10.0`, SDK `10.0.403`; версии в корневом `Directory.Packages.props`. Переход xUnit `4.0.2`/MTP `2.5.1`/AwesomeAssertions `9.6.0` уже завершён платформенным изменением R14/PR #1222. Это состояние основы, не выполненная данной задачей проверка поведения; повторная миграция runner не нужна. Старые props под `src` и JetBrains annotations не восстанавливаются. Оставшиеся OIDC/Redis/OpenSearch/CI-задачи ведутся отдельно.

До реализации уточняются конечные имена внутренних типов и новых полей конфигурации, точное DNS-кодирование и минимальная техническая форма V16. Это детали реализации в согласованных границах, не повод создавать дополнительные публичные абстракции. Сначала используется существующая тестовая инфраструктура; небольшой UDP relay допустим только для внешнего детерминированного наблюдения штатного DNS, без собственного resolver.

## Точные источники архитектурного сравнения

Источники и степень готовности сведены также в [review-v12-v13.html](review-v12-v13.html), блок `source-manifest`. Ссылки на зафиксированные ревизии ниже сохранены для проверки исходных причин; внешняя инфраструктура при синхронизации документов не вызывалась.

- [v12: регистрация клиентов](https://github.com/sitkoru/Sitko.Core/blob/fa94b44322448f85c92956f5a88c14624cc2daf1/src/Sitko.Core.Grpc.Client/ApplicationExtensions.cs).
- [v12: адрес из настроек](https://github.com/sitkoru/Sitko.Core/blob/fa94b44322448f85c92956f5a88c14624cc2daf1/src/Sitko.Core.Grpc.Client/External/ExternalGrpcClientModule.cs).
- [v12: собственный резолвер](https://github.com/sitkoru/Sitko.Core/blob/fa94b44322448f85c92956f5a88c14624cc2daf1/src/Sitko.Core.Grpc.Client/Discovery/ServiceDiscoveryGrpcClientModule.cs).
- [v12: канал и транспорт](https://github.com/sitkoru/Sitko.Core/blob/fa94b44322448f85c92956f5a88c14624cc2daf1/src/Sitko.Core.Grpc.Client/GrpcClientModule.cs).
- [v12: опции и токены](https://github.com/sitkoru/Sitko.Core/blob/fa94b44322448f85c92956f5a88c14624cc2daf1/src/Sitko.Core.Grpc.Client/GrpcClientModuleOptions.cs).
- [v12: регистрация в опциях](https://github.com/sitkoru/Sitko.Core/blob/fa94b44322448f85c92956f5a88c14624cc2daf1/src/Sitko.Core.Grpc.Server/GrpcServerModuleOptions.cs).
- [v12: сервер и публикация](https://github.com/sitkoru/Sitko.Core/blob/fa94b44322448f85c92956f5a88c14624cc2daf1/src/Sitko.Core.Grpc.Server/BaseGrpcServerModule.cs).
- [v12: модуль Consul](https://github.com/sitkoru/Sitko.Core/blob/fa94b44322448f85c92956f5a88c14624cc2daf1/src/Sitko.Core.Consul/ConsulModule.cs).
- [v12: чтение каталога](https://github.com/sitkoru/Sitko.Core/blob/fa94b44322448f85c92956f5a88c14624cc2daf1/src/Sitko.Core.Consul/ServiceDiscovery/ServiceDiscoveryManager.cs).
- [v12: модуль SD](https://github.com/sitkoru/Sitko.Core/blob/fa94b44322448f85c92956f5a88c14624cc2daf1/src/Sitko.Core.ServiceDiscovery/ServiceDiscoveryModule.cs).
- [v12: регистрация сервисов](https://github.com/sitkoru/Sitko.Core/blob/fa94b44322448f85c92956f5a88c14624cc2daf1/src/Sitko.Core.ServiceDiscovery.Server.Consul/ConsulServiceDiscoveryRegistrar.cs).
- [v12: короткое имя](https://github.com/sitkoru/Sitko.Core/blob/fa94b44322448f85c92956f5a88c14624cc2daf1/src/Sitko.Core.Grpc/GrpcHelper.cs).
- [v12: привязка конфигурации](https://github.com/sitkoru/Sitko.Core/blob/fa94b44322448f85c92956f5a88c14624cc2daf1/src/Sitko.Core.App/Helpers/OptionsHelper.cs).
- [v12: добавление модулей](https://github.com/sitkoru/Sitko.Core/blob/fa94b44322448f85c92956f5a88c14624cc2daf1/src/Sitko.Core.App/SitkoCoreBaseApplicationBuilder.cs).
- [v12: публикация портов](https://github.com/sitkoru/Sitko.Core/blob/fa94b44322448f85c92956f5a88c14624cc2daf1/src/Sitko.Core.ServiceDiscovery.Server/BaseServiceDiscoveryRegistrar.cs).
- [v12: слушатели Kestrel](https://github.com/sitkoru/Sitko.Core/blob/fa94b44322448f85c92956f5a88c14624cc2daf1/src/Sitko.Core.App.Web/AppWebConfigurationModule.cs).
- [MYCG.Core: обёртки gRPC](https://github.com/sitkoru/MYCG.Core/blob/4da7e4ae58972f0d1bef7d4f2960d6c08faf0611/src/MYCG.Core.App/CGApplicationExtensions.cs).
- [MYCG.Core: поставщик токена](https://github.com/sitkoru/MYCG.Core/blob/4da7e4ae58972f0d1bef7d4f2960d6c08faf0611/src/MYCG.Core.App/GrpcTokenProvider.cs).
- [MYCG.Core: контекст и сообщения](https://github.com/sitkoru/MYCG.Core/blob/4da7e4ae58972f0d1bef7d4f2960d6c08faf0611/src/MYCG.Core.Grpc/GrpcExtensions.cs).
- [MYCG.Core: параметры пакетной выдачи](https://github.com/sitkoru/MYCG.Core/blob/4da7e4ae58972f0d1bef7d4f2960d6c08faf0611/src/MYCG.Core.Grpc/GrpcServiceOptions.cs).
- [Microsoft: стандартный RoundRobin](https://github.com/dotnet/extensions/blob/25f07afa51034e37b334210642d7dc5f191c142d/src/Libraries/Microsoft.Extensions.ServiceDiscovery/ServiceDiscoveryServiceCollectionExtensions.cs).


## Уточнения при реализации

- Проверка связей проектов после T007: `Grpc.Client` и `Grpc.Server` ссылаются на общий `ServiceDiscovery`, без ссылки на Consul. Старые проекты `Consul`, `ServiceDiscovery.Resolver.Consul`, `ServiceDiscovery.Server.Consul`, `ServiceDiscovery.Server` и `Grpc.Client/Discovery` ещё сохраняются до T018. Поиск ссылок на старые пакеты вне `specs/` нашёл только их собственную группу и `Sitko.Core.slnx`; окончательный аудит повторяется после удаления.
- Для T012 у App уже есть `HostedLifecycleService` и модульный `ApplicationStarted`, но `ApplicationLifecycle.StartedAsync` перехватывает ошибки модульного обработчика и только пишет их в журнал. Поэтому он не подходит для обязательного отказа запуска при неверном фактическом слушателе. Если текущую фоновую валидацию потребуется перенести в гарантированную проверку старта, достаточно стандартного `IHostedLifecycleService` у владельца регистрации; App core менять не требуется. Это основание выбора при реализации, ещё не выполненное изменение.
- Проверки используют loopback-порты собственного Docker-контейнера. Текущая gRPC-задача CI запускает тесты внутри job-контейнера и имеет отдельный service Consul; он новым тестам не нужен. Перед итоговым CI требуется согласовать именно исполнение этих тестов в сетевом пространстве Docker-хоста; нельзя подменять оснастку общим `http://consul:8500`. [Документация Testcontainers](https://dotnet.testcontainers.org/dind/) отличает доступ из контейнера к Docker-хосту от loopback самого процесса. Точное изменение CI пока не реализовано.

- Уточнение T009/FR-016: binder `10.0.14` вызывает `Add` для существующих коллекций, а исходный `RetryPolicy` уже содержит коды повторов. Поэтому список контракта только с `Unavailable` не исключает `ResourceExhausted` и другие прежние коды. Требуется очистить только явно присутствующий `RetryableStatusCodes` перед двумя существующими Bind; отсутствующий список и остальные поля политики сохраняются. Это исправление уже согласованного приоритета, без общего механизма слияния. Регрессия использует существующий реальный TLS RPC: исключённый код возвращает ошибку, разрешённый повтор возвращает заданный ответ. Источники: [точная версия binder](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/libraries/Microsoft.Extensions.Configuration.Binder/src/ConfigurationBinder.cs#L802-L827), [коллекция gRPC](https://github.com/grpc/grpc-dotnet/blob/v2.84.0/src/Grpc.Net.Client/Internal/Configuration/Values.cs#L58-L68).
