# Логи, метрики и трассировка

[English](../en/observability.md) · [Все темы](README.md)

`Sitko.Core.App` использует Serilog и автоматически регистрирует
`OpenTelemetryModule` при сборке контейнера. Отдельный пакет логирования Sitko.Core
не требуется. `ILogger<T>` доступен из стандартного DI; `ConfigureLogging(...)`
и `ConfigureLogLevel(...)` у Sitko.Core builder настраивают Serilog.

## Экспорт OTLP

```json
{
  "Application": { "Name": "example-api", "EnableConsoleLogging": true },
  "OpenTelemetry": {
    "EnableOtlpExport": true,
    "Endpoint": "http://localhost:4317",
    "OtlpExportProtocol": "Grpc",
    "Headers": {
      "Authorization": "Bearer example-token",
      "organization": "example"
    },
    "EnableLogs": true,
    "EnableMetrics": true,
    "EnableTracing": true
  }
}
```

Пример предназначен для локального приёмника OTLP/gRPC с условными реквизитами.
Он должен быть доступен из процесса приложения; настройка не разворачивает
приёмник или хранилище. `Headers` по умолчанию пуст: запросы отправляются без
прикладных заголовков авторизации. Подставляйте реквизиты своего приёмника через
обычную конфигурацию .NET, включая Vault или `OpenTelemetry__Headers__Authorization`.
Имена существующих полей и флагов сохраняются.

Без `EnableOtlpExport=true` экспорт выключен. `EnableLogs`, `EnableMetrics` и
`EnableTracing` по умолчанию равны `true` и управляют соответствующим экспортом;
`Enabled=false` отключает весь модуль. Окружение `OTEL_EXPORTER_OTLP_*` не включает
выключенный модуль, экспорт или отдельный сигнал. Другие модули могут независимо
регистрировать инструментацию. Консоль управляется `Application:EnableConsoleLogging`.

Для `Grpc` задавайте адрес приёмника, например `http://localhost:4317`.
Для `HttpProtobuf` задавайте **базовый URL**, например `http://localhost:4318/tenant`:
модуль отправляет логи в `/tenant/v1/logs`, метрики в `/tenant/v1/metrics`, трассы
в `/tenant/v1/traces`. Конечный `/v1/{signal}` в базовый URL добавлять не нужно.
Это правило одинаково для `OpenTelemetry:Endpoint` и общего
`OTEL_EXPORTER_OTLP_ENDPOINT`; [переход с точного HTTP URI](migration-v13.md#экспорт-otel)
требует обновления конфигурации.

## Приоритет источников

После обычной привязки настроек .NET и программной настройки параметров модуля
непустые общие `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_PROTOCOL` и
`OTEL_EXPORTER_OTLP_HEADERS` переопределяют **соответствующее поле** для всех трёх
сигналов. При отсутствии переменной либо пустой строке используется раздел
`OpenTelemetry`. Например, env с одними заголовками не меняет адрес или протокол.
Соединения перенастраиваются после перезапуска приложения.

Непустой `OTEL_EXPORTER_OTLP_HEADERS` заменяет **весь** словарь `Headers`.
Заголовки проигравшего источника не добавляются к выбранным реквизитам.
Формат env — пары `name=value`, разделённые запятыми; percent encoding декодируется
по правилам SDK. Например, условное значение
`Authorization=Bearer%20example-token,organization=example` задаёт два заголовка.
Пустая строка env не очищает словарь; для отправки без заголовков оставьте `Headers`
пустым и не задавайте непустой `OTEL_EXPORTER_OTLP_HEADERS`.

Конечный адрес должен быть абсолютным HTTP/HTTPS URL, протокол — `Grpc` либо
`HttpProtobuf` в разделе и `grpc` либо `http/protobuf` в env. В разделе `Endpoint`
может отсутствовать, если корректный адрес пришёл из env. Проверка выполняется
для включённого экспорта; неверные конечные параметры отклоняются без вывода
имён и значений заголовков. Имена заголовков должны быть HTTP token, значения —
печатными ASCII без запятых и управляющих символов. Ограничение запятых связано
с форматом SDK 1.20.1: он декодирует всю строку до разделения пар. Percent encoding
не позволяет передать запятую внутри значения. Не записывайте реквизиты в журналы.

## Поддерживаемые стандартные переменные

Таблица относится к встроенным экспортёрам текущего стека:
OpenTelemetry `1.20.1` с `AddOtlpExporter` для метрик/трасс и
Serilog.Sinks.OpenTelemetry `4.2.0` для логов. Она не обещает поддержку любого
суффикса `OTEL_EXPORTER_OTLP_*`.

| Имя | Логи | Метрики | Трассы |
| --- | --- | --- | --- |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Общий адрес | Общий адрес | Общий адрес |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | Общий протокол | Общий протокол | Общий протокол |
| `OTEL_EXPORTER_OTLP_HEADERS` | Полная замена заголовков | Полная замена заголовков | Полная замена заголовков |
| `OTEL_EXPORTER_OTLP_TIMEOUT` | Не поддерживается sink | Штатный SDK | Штатный SDK |
| `OTEL_EXPORTER_OTLP_COMPRESSION` | Не поддерживается sink | Штатный SDK | Штатный SDK |
| `OTEL_EXPORTER_OTLP_CERTIFICATE` | Не поддерживается sink | Штатный SDK | Штатный SDK |
| `OTEL_EXPORTER_OTLP_CLIENT_CERTIFICATE` | Не поддерживается sink | Штатный SDK | Штатный SDK |
| `OTEL_EXPORTER_OTLP_CLIENT_KEY` | Не поддерживается sink | Штатный SDK | Штатный SDK |
| `OTEL_EXPORTER_OTLP_METRICS_TEMPORALITY_PREFERENCE` | — | Штатный SDK | — |
| `OTEL_EXPORTER_OTLP_METRICS_DEFAULT_HISTOGRAM_AGGREGATION` | — | Штатный SDK | — |
| `OTEL_EXPORTER_OTLP_LOGS_ENDPOINT`, `OTEL_EXPORTER_OTLP_LOGS_PROTOCOL`, `OTEL_EXPORTER_OTLP_LOGS_HEADERS`, `OTEL_EXPORTER_OTLP_LOGS_TIMEOUT`, `OTEL_EXPORTER_OTLP_LOGS_COMPRESSION` | Не поддерживаются | — | — |
| `OTEL_EXPORTER_OTLP_METRICS_ENDPOINT`, `OTEL_EXPORTER_OTLP_METRICS_PROTOCOL`, `OTEL_EXPORTER_OTLP_METRICS_HEADERS`, `OTEL_EXPORTER_OTLP_METRICS_TIMEOUT`, `OTEL_EXPORTER_OTLP_METRICS_COMPRESSION` | — | Не поддерживаются этим API | — |
| `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT`, `OTEL_EXPORTER_OTLP_TRACES_PROTOCOL`, `OTEL_EXPORTER_OTLP_TRACES_HEADERS`, `OTEL_EXPORTER_OTLP_TRACES_TIMEOUT`, `OTEL_EXPORTER_OTLP_TRACES_COMPRESSION` | — | — | Не поддерживаются этим API |

Signal-specific адреса/протоколы/заголовки/тайм-ауты/сжатие SDK поддерживает только
через `UseOtlpExporter`; встроенный модуль использует `AddOtlpExporter` и Serilog.
Переход на другой API не является частью настройки модуля. Прочие штатные env
SDK, включая `OTEL_BSP_*`, `OTEL_METRIC_EXPORT_*` и атрибуты ресурсов, не отключаются.
Настройки сертификатов остаются возможностью SDK; эта реализация не добавляет
их в Serilog sink. TLS/mTLS и хранение данных проверяйте на своём приёмнике отдельно.

## Ресурсы, программная настройка и проверка

В ресурсах используются имя приложения, `deployment.environment` и `host.name`.
Штатные `OTEL_RESOURCE_ATTRIBUTES` и `OTEL_SERVICE_NAME` продолжают читаться
библиотеками. SDK `AddService` закрепляет имя приложения; sink применяет
`OTEL_SERVICE_NAME` после ресурсов модуля. Этот порядок независим от общего
приоритета настроек подключения.

Базовая трассировка включает источники `Sitko.*` и HttpClient; подключённые
модули могут добавлять свою инструментацию. `ConfigureOpenTelemetry(...)`
дополняет SDK регистрацию, например источники, счётчики и ресурсы. Он вызывается
после настройки логирования: не используйте изменение его `options` для общей
перенастройки адреса или авторизации. Прямые поздние SDK callbacks подчиняются
порядку SDK; приоритет env над произвольной дополнительной настройкой SDK не
гарантируется. Не регистрируйте второй `OpenTelemetryModule` поверх автоматического.

Проверка: выполните запрос приложения с `ILogger<T>`, собственной метрикой и
трассировкой, затем проверьте все три сигнала в своём приёмнике по имени приложения.
Успешный запуск не подтверждает приём/сохранение телеметрии. Доступность зависимостей
проверяйте с учётом [тегов HTTP-проверок](application.md).
Локальные [интеграционные тесты](../../tests/Sitko.Core.App.Tests/OpenTelemetryExportTests.cs)
проверяют реальный приём с авторизацией, конфликт источников и отключение;
production OpenObserve этими тестами не проверяется.

Источники: [модуль](../../src/Sitko.Core.App/OpenTelemetry/OpenTelemetryModule.cs),
[параметры](../../src/Sitko.Core.App/OpenTelemetry/OpenTelemetryModuleOptions.cs),
[разрешение настроек](../../src/Sitko.Core.App/OpenTelemetry/OtlpExportSettings.cs),
[SDK 1.20.1](https://github.com/open-telemetry/opentelemetry-dotnet/blob/core-1.20.1/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/README.md),
[sink 4.2.0](https://github.com/serilog/serilog-sinks-opentelemetry/blob/v4.2.0/src/Serilog.Sinks.OpenTelemetry/Sinks/OpenTelemetry/Configuration/OpenTelemetryEnvironment.cs).
