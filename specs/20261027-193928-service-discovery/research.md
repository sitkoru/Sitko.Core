# Исследование: обнаружение gRPC-сервисов

Дата: 2026-10-27. Базовая ревизия Sitko.Core: `fa94b44322448f85c92956f5a88c14624cc2daf1`. Это подготовительные сведения, а не свидетельство выполненной реализации.

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

Текущий grpc-dotnet — 2.80.0; версия 2.84.0 использовалась только в прежних прототипах. Прототипы не проверяли TLS/токены и ответ 256 МиБ в Sitko.Core. Обновление пакета не является самостоятельным требованием.

## Идентичность и регистрация

**Решение**: полное protobuf-имя, отдельная регистрация контракта/экземпляра, окружение в DNS-имени; смешанный переход не поддерживается.

**Основание**: `GrpcServicesHelper` сейчас использует `DeclaringType.Name`, что смешивает короткие имена разных protobuf-пакетов. Старый `ServiceDiscoveryManager` публикует `приложение_порт` и собственные метаданные, а SRV-клиенту нужна регистрация самого логического контракта. Простая замена клиентского resolver оставила бы FR-002/003 невыполненными.

В свежем MYCG.Core `CGApplicationExtensions.AddGrpcClient` использует прежний `GetServiceNameForClient<TClient>()` для поиска ключа в `GrpcClientsAddresses`. Изменение результата этого helper незаметно переключило бы статического клиента на SD. Поэтому его семантика сохраняется, а новая идентичность получается отдельными helper. Это минимальное сохранение публичной функции, а не поддержка старой discovery-схемы.

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

Текущий SD-тест обращается к `localhost:8500`, использует фиксированный диапазон портов и проверяет поздний старт сервера. Требуются изолированные реальные проверки после подтверждения пользователем их границ. В репозитории уже есть Testcontainers 4.6.0 и генерация gRPC `.proto`; новая система проверки не нужна.

На этапе исследования не запускались тесты, приложения или контейнеры. Windows/Linux, производственная конфигурация, реальное восстановление TLS-клиента Sitko.Core и производительность больших ответов ещё не проверены.
