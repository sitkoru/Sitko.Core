# Logs, metrics and tracing

[Русский](../ru/observability.md) · [All topics](README.md)

`Sitko.Core.App` uses Serilog and automatically registers `OpenTelemetryModule`
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

This example targets a local OTLP/gRPC receiver with placeholder credentials.
The receiver must be reachable from the application; configuration does not
deploy a receiver or storage. `Headers` defaults to empty: requests carry no
application authentication headers. Supply receiver credentials through ordinary
.NET configuration, including Vault or `OpenTelemetry__Headers__Authorization`.
Existing field and flag names are retained.

Export is disabled unless `EnableOtlpExport=true`. `EnableLogs`, `EnableMetrics`
and `EnableTracing` default to `true` and control the corresponding export;
`Enabled=false` disables the module. `OTEL_EXPORTER_OTLP_*` cannot enable a disabled
module, export or signal. Other modules can register instrumentation independently.
The console is controlled by `Application:EnableConsoleLogging`.

For `Grpc`, use the receiver address, such as `http://localhost:4317`.
For `HttpProtobuf`, use a **base URL**, such as `http://localhost:4318/tenant`:
the module sends logs to `/tenant/v1/logs`, metrics to `/tenant/v1/metrics`, and
traces to `/tenant/v1/traces`. Do not include the final `/v1/{signal}` in this URL.
This applies equally to `OpenTelemetry:Endpoint` and the common
`OTEL_EXPORTER_OTLP_ENDPOINT`; [migration from exact HTTP URIs](migration-v13.md#otel-export)
requires a configuration update.

## Source precedence

After ordinary .NET binding and programmatic module option configuration,
nonempty common `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_PROTOCOL`, and
`OTEL_EXPORTER_OTLP_HEADERS` override the **corresponding field** for all three
signals. Missing variables and empty strings fall back to `OpenTelemetry`.
For example, setting only environment headers does not change the endpoint or
protocol. Restart the application to change exporter connections.

A nonempty `OTEL_EXPORTER_OTLP_HEADERS` replaces the **entire** `Headers` dictionary.
Headers from the other source are not merged into the selected credentials.
The environment format is comma-separated `name=value` pairs with percent decoding
according to the SDK. The placeholder value
`Authorization=Bearer%20example-token,organization=example` specifies two headers.
An empty environment string does not clear the dictionary: leave `Headers` empty
and do not set a nonempty `OTEL_EXPORTER_OTLP_HEADERS` to send without headers.

The final endpoint must be an absolute HTTP/HTTPS URL. Protocol values are `Grpc`
or `HttpProtobuf` in configuration and `grpc` or `http/protobuf` in the environment.
The configuration `Endpoint` may be omitted when a valid environment endpoint is
provided. Enabled export validates the final settings; errors do not reveal header
names or values. Header names must be HTTP tokens; values must be printable ASCII
without commas or control characters. The comma restriction comes from SDK 1.20.1,
which decodes the whole string before splitting pairs. Percent encoding cannot
represent a comma inside a value. Do not write credentials to logs.

## Supported standard variables

This table describes the built-in exporters in the current stack:
OpenTelemetry `1.20.1` with `AddOtlpExporter` for metrics/traces and
Serilog.Sinks.OpenTelemetry `4.2.0` for logs. It does not promise support for every
`OTEL_EXPORTER_OTLP_*` suffix.

| Name | Logs | Metrics | Traces |
| --- | --- | --- | --- |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Common endpoint | Common endpoint | Common endpoint |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | Common protocol | Common protocol | Common protocol |
| `OTEL_EXPORTER_OTLP_HEADERS` | Complete header replacement | Complete header replacement | Complete header replacement |
| `OTEL_EXPORTER_OTLP_TIMEOUT` | Unsupported by sink | Native SDK | Native SDK |
| `OTEL_EXPORTER_OTLP_COMPRESSION` | Unsupported by sink | Native SDK | Native SDK |
| `OTEL_EXPORTER_OTLP_CERTIFICATE` | Unsupported by sink | Native SDK | Native SDK |
| `OTEL_EXPORTER_OTLP_CLIENT_CERTIFICATE` | Unsupported by sink | Native SDK | Native SDK |
| `OTEL_EXPORTER_OTLP_CLIENT_KEY` | Unsupported by sink | Native SDK | Native SDK |
| `OTEL_EXPORTER_OTLP_METRICS_TEMPORALITY_PREFERENCE` | — | Native SDK | — |
| `OTEL_EXPORTER_OTLP_METRICS_DEFAULT_HISTOGRAM_AGGREGATION` | — | Native SDK | — |
| `OTEL_EXPORTER_OTLP_LOGS_ENDPOINT`, `OTEL_EXPORTER_OTLP_LOGS_PROTOCOL`, `OTEL_EXPORTER_OTLP_LOGS_HEADERS`, `OTEL_EXPORTER_OTLP_LOGS_TIMEOUT`, `OTEL_EXPORTER_OTLP_LOGS_COMPRESSION` | Unsupported | — | — |
| `OTEL_EXPORTER_OTLP_METRICS_ENDPOINT`, `OTEL_EXPORTER_OTLP_METRICS_PROTOCOL`, `OTEL_EXPORTER_OTLP_METRICS_HEADERS`, `OTEL_EXPORTER_OTLP_METRICS_TIMEOUT`, `OTEL_EXPORTER_OTLP_METRICS_COMPRESSION` | — | Unsupported by this API | — |
| `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT`, `OTEL_EXPORTER_OTLP_TRACES_PROTOCOL`, `OTEL_EXPORTER_OTLP_TRACES_HEADERS`, `OTEL_EXPORTER_OTLP_TRACES_TIMEOUT`, `OTEL_EXPORTER_OTLP_TRACES_COMPRESSION` | — | — | Unsupported by this API |

The SDK supports signal-specific endpoints/protocols/headers/timeouts/compression
only through `UseOtlpExporter`; this module uses `AddOtlpExporter` and Serilog.
Switching APIs is outside module configuration. Other native SDK environment
settings, including `OTEL_BSP_*`, `OTEL_METRIC_EXPORT_*`, and resource attributes,
are not disabled. Certificate settings remain SDK features; this implementation
does not add them to the Serilog sink. Verify TLS/mTLS and storage separately
against your receiver.

## Resources, programmatic configuration and verification

Resources include the application name, `deployment.environment`, and `host.name`.
The libraries still read native `OTEL_RESOURCE_ATTRIBUTES` and `OTEL_SERVICE_NAME`.
SDK `AddService` fixes the application service name; the sink applies
`OTEL_SERVICE_NAME` after module resource attributes. This ordering is independent
of the shared connection-setting precedence.

Base tracing includes `Sitko.*` sources and HttpClient instrumentation; connected
modules may add their own instrumentation. `ConfigureOpenTelemetry(...)` extends
SDK registration, such as sources, meters, and resources. It runs after logging
configuration: do not mutate its `options` to change shared endpoints or credentials.
Direct late SDK callbacks follow SDK ordering; environment precedence over arbitrary
additional SDK configuration is not guaranteed. Do not register a second
`OpenTelemetryModule` in addition to the automatic one.

Verify by making an application request that logs through `ILogger<T>`, records
a custom metric, and creates a trace, then inspect all three signals in your
receiver by application name. Successful startup does not prove telemetry reception
or storage. Check dependency availability with the [HTTP health-check tags](application.md).
Local [integration tests](../../tests/Sitko.Core.App.Tests/OpenTelemetryExportTests.cs)
verify real authenticated reception, source conflicts, and disabled export;
they do not verify production OpenObserve.

Sources: [module](../../src/Sitko.Core.App/OpenTelemetry/OpenTelemetryModule.cs),
[options](../../src/Sitko.Core.App/OpenTelemetry/OpenTelemetryModuleOptions.cs),
[setting resolution](../../src/Sitko.Core.App/OpenTelemetry/OtlpExportSettings.cs),
[SDK 1.20.1](https://github.com/open-telemetry/opentelemetry-dotnet/blob/core-1.20.1/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/README.md),
[sink 4.2.0](https://github.com/serilog/serilog-sinks-opentelemetry/blob/v4.2.0/src/Serilog.Sinks.OpenTelemetry/Sinks/OpenTelemetry/Configuration/OpenTelemetryEnvironment.cs).
