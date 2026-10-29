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
    "EnableLogs": true,
    "EnableMetrics": true,
    "EnableTracing": true
  }
}
```

Пример предназначен для локального коллектора OTLP/gRPC. Он должен быть доступен
из процесса приложения; настройка не разворачивает коллектор или хранилище.
Без `EnableOtlpExport=true` экспорт выключен, хотя метрики и трассировка
по умолчанию включены. `EnableLogs` управляет OTLP-логами, а консоль —
`Application:EnableConsoleLogging`. Для `HttpProtobuf` согласуйте Endpoint с
приёмником; модуль передаёт настроенный адрес экспортёрам.

В ресурсах используются имя приложения, `deployment.environment` и `host.name`.
Базовая трассировка включает источники `Sitko.*` и HttpClient; подключённые
модули могут добавлять собственную инструментацию. `ConfigureOpenTelemetry(...)`
позволяет дополнить регистрацию. Не регистрируйте второй `OpenTelemetryModule`
вручную поверх автоматического.

Проверка: выполните запрос приложения с `ILogger<T>` и HTTP-вызовом, затем
проверьте запись и трассу в своём приёмнике по имени приложения. Успешный запуск
процесса не подтверждает приём/сохранение телеметрии. Доступность зависимостей
проверяйте с учётом [тегов HTTP-проверок](application.md).

Источники: [модуль](../../src/Sitko.Core.App/OpenTelemetry/OpenTelemetryModule.cs),
[параметры](../../src/Sitko.Core.App/OpenTelemetry/OpenTelemetryModuleOptions.cs),
[Serilog](../../src/Sitko.Core.App/Logging/SerilogConfigurator.cs).
