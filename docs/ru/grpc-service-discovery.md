# gRPC и обнаружение сервисов

[English](../en/grpc-service-discovery.md)

`Sitko.Core.Grpc.Client` вызывает сервис через Microsoft Service Discovery и DNS SRV. `Sitko.Core.ServiceDiscovery.Consul` публикует серверные контракты в Consul и настраивает DNS. Клиентский и серверный gRPC-пакеты не зависят от Consul; приложение подключает провайдер отдельно.

## Регистрация

Сервер подключает провайдер и объявляет реализации контрактов:

```csharp
var app = builder.AddSitkoCoreWeb();
app.AddConsulServiceDiscovery();
app.AddGrpcServer()
    .RegisterService<InvoiceGrpcService>()
    .RegisterService<ReportsGrpcService>();
```

Клиенту достаточно знать сгенерированный protobuf-тип:

```csharp
var app = builder.AddSitkoCore();
app.AddConsulServiceDiscovery();
app.AddGrpcClient<Invoice.InvoiceClient>();
```

Пакет приложения может включать `AddGrpcClient` в собственный метод подключения модуля. Регистрации выполняются до `Build`; добавление новых модулей внутри `ConfigureHostBuilder` уже во время сборки контейнера не поддерживается текущим App. Клиентскому приложению не нужен веб-сервер.

Полное protobuf-имя, например `accounting.v1.Invoice`, используется и для идентичности контракта, и для имени клиента в штатной фабрике gRPC. Одинаковые короткие C#-имена из разных protobuf-пакетов не смешиваются. Имя приложения клиенту не требуется.

## Consul и адрес сервера

Пример для отдельных процессов на одном хосте:

```json
{
  "Application": {
    "Environment": "local",
    "Web": {
      "Ports": {
        "gRPC": {
          "Port": 0,
          "Protocol": "Http2",
          "UseTLS": true,
          "ExternalAddress": "127.0.0.1"
        }
      }
    }
  },
  "ServiceDiscovery": {
    "Consul": {
      "Address": "http://127.0.0.1:8500",
      "DnsServers": ["127.0.0.1:8600"]
    }
  }
}
```

Клиент использует те же `Application:Environment` и `ServiceDiscovery:Consul`; секция `Web` ему не нужна. `Port=0` поручает выбор порта операционной системе. После запуска Kestrel публикуется фактически занятый порт именованного TLS/HTTP2-слушателя. `ExternalAddress` — адрес для клиентов, а не ограничение интерфейса прослушивания. Для Kubernetes укажите доступный Pod IP; loopback подходит только процессам того же хоста.

Каждый контракт экземпляра получает собственную запись Consul. Несколько контрактов могут использовать один TLS-порт. Пара окружение/полное имя преобразуется в общую DNS-метку, исходные значения сохраняются в метаданных. Регистрация и TTL обращаются к одному настроенному агенту. Consul DNS должен исключать экземпляры с критическим состоянием проверки.

`DnsServers` задаёт адрес и порт DNS, отдельно от HTTP `Address` агента. Системные DNS-настройки приложения не меняются. При недоступности Consul вызовы могут завершаться ошибками; сохранение последних известных адресов не гарантируется.

## Прямой адрес и выключение SD

Способ вызова определяется настройкой одного клиента:

```json
{
  "Grpc": {
    "Client": {
      "Contracts": {
        "accounting.v1.Invoice": {
          "Address": "https://invoices.example:7443"
        }
      }
    }
  }
}
```

При корректном `Address` клиент обращается напрямую и не требует провайдера SD. Без `Address` он использует обнаружение. Некорректный адрес, отключённый SD или отсутствие провайдера при необходимости обнаружения вызывают ошибку запуска с именем контракта. Ошибка прямого вызова не переключает клиент на SD. Менять регистрацию `AddGrpcClient<T>` и пересобирать приложение для смены режима не требуется.

Единственный `ServiceDiscovery:Enabled=false` отключает поиск, публикацию и TTL. Прямые клиенты и серверные RPC продолжают работать. Самостоятельный сервер без подключения SD не требует провайдера. Если приложение явно подключает `AddServiceDiscovery()` для публикации серверных контрактов, нужен провайдер; пустая реализация не подставляется.

## Интервалы провайдера

Поля находятся в `ServiceDiscovery:Consul`. Изменения применяются после перезапуска приложения.

| Параметр | Назначение |
| --- | --- |
| `DnsTimeout`, `DnsMaxAttempts` | Ограничение одной DNS-попытки и число попыток для каждого DNS-сервера |
| `DnsRefreshPeriod` | Период обновления DNS при отсутствии собственного TTL ответа |
| `DnsMinRetryPeriod`, `DnsMaxRetryPeriod`, `DnsRetryBackOffFactor` | Задержки повторного разрешения и множитель |
| `Ttl`, `HeartbeatInterval` | Время жизни проверки экземпляра и интервал подтверждения |
| `RequestTimeout`, `RegistrationRetryInterval` | Ограничение HTTP-запроса агента и интервал восстановления регистрации |
| `DeregisterCriticalServiceAfter` | Задержка удаления критической регистрации самим Consul |

Интервалы и число попыток должны быть положительными, heartbeat — меньше TTL, максимальная задержка DNS — не меньше начальной. Исключение умершего экземпляра из здоровых DNS-результатов и физическое удаление его записи — разные события. Политика повторов RPC настраивается отдельно от этих интервалов. `DnsTimeout` не ограничивает весь RPC: один запрос DNS может последовательно использовать несколько попыток и DNS-серверов. Ограничивайте сам вызов через deadline или отмену.

## Общие и индивидуальные лимиты

Пределы задаются в байтах. Например, эта конфигурация разрешает клиенту и серверу
принимать до 1 МиБ по умолчанию и до 4 МиБ для `accounting.v1.Invoice`:

```json
{
  "Grpc": {
    "Client": {
      "Default": { "MaxReceiveMessageSize": 1048576 },
      "Contracts": {
        "accounting.v1.Invoice": { "MaxReceiveMessageSize": 4194304 }
      }
    },
    "Server": {
      "Default": { "MaxReceiveMessageSize": 1048576 },
      "Contracts": {
        "accounting.v1.Invoice": { "MaxReceiveMessageSize": 4194304 }
      }
    }
  }
}
```

`MaxSendMessageSize` отдельно ограничивает отправку. Для успешного вызова должны
подходить предел отправителя и предел получателя. Размер относится к protobuf-сообщению,
поэтому полезные данные ровно на границе могут превысить лимит за счёт служебных полей.

Настройки применяются последовательно: значения SDK → `Default` → общий обработчик
в коде → `Contracts` → индивидуальный обработчик в коде. Отсутствующее поле следующего
уровня сохраняет предыдущее значение. Общий клиентский обработчик добавляется через
`ConfigureGrpcClients(options => ...)`; индивидуальный — через `AddGrpcClient<T>(options => ...)`.

Общую серверную настройку SDK задаёт `ConfigureGrpcService`; индивидуальный
обработчик `RegisterService` получает `GrpcServiceOptions` напрямую:

```csharp
app.AddGrpcServer(options =>
    options.ConfigureGrpcService = grpc => grpc.MaxReceiveMessageSize = 1048576)
    .RegisterService<InvoiceGrpcService>(grpc => grpc.MaxReceiveMessageSize = 4194304)
    .RegisterService<ReportsGrpcService>();
```

Повторные `AddGrpcServer` дополняют один сервер приложения. Все контракты получают
итоговые общие настройки, включая обработчики, добавленные после регистрации
сервиса. Повтор одного типа не создаёт второй маршрут; другой тип реализации того
же protobuf-контракта вызывает ошибку. Для gRPC-Web используйте
`RegisterServiceWithGrpcWeb<TService>()`. `RequireSchemeAuthorization(...)` в общих
настройках сервера сохраняет требование указанной схемы аутентификации.

## TLS и токены

TLS остаётся частью gRPC-транспорта. DNS выбирает адрес и порт; он не заменяет настройку сертификата и доверия к нему. Существующие `ConfigureHttpHandler`, `ConfigureChannelOptions`, поставщики токена и метаданных остаются точками настройки клиента. `DisableCertificatesValidation` явно отключает проверку сертификата, когда это предусмотрено конфигурацией приложения.

`AddTokenAuth<TProvider>()` подключает поставщик `IGrpcTokenProvider`. Поставщик возвращает значение заголовка `Authorization`, включая нужную схему, например `Bearer …`; оно запрашивается при RPC. Передача credentials по незащищённому соединению автоматически не включается. Входящая авторизация сервера настраивается отдельно.

Общая политика применяется и к клиентам, зарегистрированным до неё:

```csharp
app.ConfigureGrpcClients(options => options.AddTokenAuth<ApplicationTokenProvider>());
app.AddGrpcClient<Invoice.InvoiceClient>();
app.AddGrpcClient<Reports.ReportsClient>(options => options.AddTokenAuth<ReportsTokenProvider>());
```

Поставщики в примере реализуют `IGrpcTokenProvider`. Индивидуальный поставщик
заменяет общий. Для заданного времени жизни заранее зарегистрируйте конкретный
тип поставщика в DI, например `services.AddScoped<ApplicationTokenProvider>()`;
`AddTokenAuth` сохраняет эту регистрацию. Без неё используется transient.
Поставщик разрешается из DI-области клиента при каждом RPC, поэтому изменение
токена не требует нового клиента или канала. Явная
`IGrpcTokenProviderFactory<TClient>` имеет приоритет над общим поставщиком.

Через `ConfigureGrpcClients` также доступны общие `AddMetadataProvider`,
`AddInterceptor`, `ConfigureHttpHandler` и `ConfigureChannelOptions`.

Основные точки реализации: [gRPC-клиент](../../src/Sitko.Core.Grpc.Client/GrpcClientModule.cs), [сервер](../../src/Sitko.Core.Grpc.Server/GrpcServerModule.cs), [провайдер Consul](../../src/Sitko.Core.ServiceDiscovery.Consul/ConsulServiceDiscoveryModule.cs). Требования перехода собраны в [спецификации](../../specs/20261027-193928-service-discovery/spec.md).
