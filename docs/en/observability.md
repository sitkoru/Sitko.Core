# Logs, metrics and tracing

[Русский](../ru/observability.md) · [All topics](README.md)

`Sitko.Core.App` uses Serilog and registers `OpenTelemetryModule` automatically
when building the container. No separate Sitko.Core logging package is required.
`ILogger<T>` is available through standard DI; the Sitko.Core builder's
`ConfigureLogging(...)` and `ConfigureLogLevel(...)` configure Serilog.

## OTLP export

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

This example targets a local OTLP/gRPC collector reachable from the application.
It does not deploy a collector or storage. Export is disabled unless
`EnableOtlpExport=true`, although metrics and tracing are enabled by default.
`EnableLogs` controls OTLP logs; `Application:EnableConsoleLogging` controls
the console. For `HttpProtobuf`, match Endpoint to the receiver; the module passes
the configured address to its exporters.

Resources include the application name, `deployment.environment` and `host.name`.
Base tracing includes `Sitko.*` sources and HttpClient instrumentation; connected
modules may add their own instrumentation. Use `ConfigureOpenTelemetry(...)`
to extend registration. Do not register a second `OpenTelemetryModule` manually
in addition to the automatic one.

Verify by running an application request that logs through `ILogger<T>` and makes
an HTTP call, then check the record and trace in your receiver by application name.
Successful startup does not prove telemetry reception or storage. Check dependency
availability with the [HTTP health-check tags](application.md) in mind.

Sources: [module](../../src/Sitko.Core.App/OpenTelemetry/OpenTelemetryModule.cs),
[options](../../src/Sitko.Core.App/OpenTelemetry/OpenTelemetryModuleOptions.cs),
[Serilog](../../src/Sitko.Core.App/Logging/SerilogConfigurator.cs).
