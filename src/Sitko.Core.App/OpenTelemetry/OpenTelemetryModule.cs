using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace Sitko.Core.App.OpenTelemetry;

public class OpenTelemetryModule : BaseApplicationModule<OpenTelemetryModuleOptions>,
    ILoggingModule<OpenTelemetryModuleOptions>
{
    public override string OptionsKey => "OpenTelemetry";

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        OpenTelemetryModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);

        var otelBuilder = services.AddOpenTelemetry();
        otelBuilder
            .ConfigureResource(builder => builder.AddService(applicationContext.Name).AddAttributes([
                new KeyValuePair<string, object>("deployment.environment", applicationContext.Environment),
                new KeyValuePair<string, object>("host.name", Environment.MachineName)
            ]));

        if (startupOptions.EnableTracing)
        {
            otelBuilder.WithTracing(builder =>
            {
                builder.AddSource("Sitko.*");
                builder.AddHttpClientInstrumentation();
                if (startupOptions.EnableOtlpExport)
                {
                    builder.AddOtlpExporter(exporterBuilder =>
                        OtlpExportSettings.Resolve(startupOptions).Apply(exporterBuilder, "traces"));
                }
            });
        }

        if (startupOptions.EnableMetrics)
        {
            otelBuilder.WithMetrics(builder =>
            {
                builder.AddHttpClientInstrumentation();
                if (startupOptions.EnableOtlpExport)
                {
                    builder.AddOtlpExporter(exporterBuilder =>
                        OtlpExportSettings.Resolve(startupOptions).Apply(exporterBuilder, "metrics"));
                }
            });
        }

        foreach (var configureAction in startupOptions.ConfigureActions)
        {
            configureAction.Invoke(applicationContext, startupOptions, otelBuilder);
        }
    }

    public LoggerConfiguration ConfigureLogging(IApplicationContext context, OpenTelemetryModuleOptions options,
        LoggerConfiguration loggerConfiguration)
    {
        if (options is { EnableOtlpExport: true, EnableLogs: true })
        {
            var settings = OtlpExportSettings.Resolve(options);
            loggerConfiguration
                .Enrich.WithProperty("ApplicationId", context.Id)
                .Enrich.WithProperty("ApplicationName", context.Name)
                .Enrich.WithProperty("ApplicationVersion", context.Version)
                .WriteTo.OpenTelemetry(sinkOptions =>
                {
                    sinkOptions.Protocol = settings.Protocol switch
                    {
                        OtlpExportProtocol.Grpc => OtlpProtocol.Grpc,
                        OtlpExportProtocol.HttpProtobuf => OtlpProtocol.HttpProtobuf,
                        _ => throw new InvalidOperationException("Unsupported OTLP protocol.")
                    };
                    sinkOptions.Headers = settings.Headers;
                    sinkOptions.Endpoint = null;
                    sinkOptions.LogsEndpoint = settings.GetEndpoint("logs").AbsoluteUri;
                    sinkOptions.TracesEndpoint = settings.GetEndpoint("traces").AbsoluteUri;
                    sinkOptions.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = context.Name,
                        ["deployment.environment"] = context.Environment,
                        ["host.name"] = Environment.MachineName
                    };
                }, getConfigurationVariable: static name => name is
                    "OTEL_EXPORTER_OTLP_ENDPOINT" or "OTEL_EXPORTER_OTLP_PROTOCOL" or "OTEL_EXPORTER_OTLP_HEADERS"
                    ? null
                    : Environment.GetEnvironmentVariable(name));
        }

        return loggerConfiguration;
    }
}
