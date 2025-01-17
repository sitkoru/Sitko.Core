using System.ComponentModel;
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
            .ConfigureResource(builder => builder.AddService(applicationContext.Name));

        if (startupOptions.EnableTracing)
        {
            otelBuilder.WithTracing(builder =>
            {
                builder.AddSource("Sitko.*");
                builder.AddHttpClientInstrumentation();
                if (startupOptions.EnableOtlpExport)
                {
                    builder.AddOtlpExporter(exporterBuilder =>
                    {
                        exporterBuilder.Endpoint = startupOptions.Endpoint!;
                        exporterBuilder.Protocol = startupOptions.OtlpExportProtocol;
                    });
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
                    {
                        exporterBuilder.Endpoint = startupOptions.Endpoint!;
                        exporterBuilder.Protocol = startupOptions.OtlpExportProtocol;
                    });
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
            loggerConfiguration
                .Enrich.WithProperty("ApplicationId", context.Id)
                .Enrich.WithProperty("ApplicationName", context.Name)
                .Enrich.WithProperty("ApplicationVersion", context.Version)
                .WriteTo.OpenTelemetry(sinkOptions =>
                {
                    sinkOptions.Protocol = options.OtlpExportProtocol switch
                    {
                        OtlpExportProtocol.Grpc => OtlpProtocol.Grpc,
                        OtlpExportProtocol.HttpProtobuf => OtlpProtocol.HttpProtobuf,
                        _ => throw new InvalidEnumArgumentException($"Unknown value {options.OtlpExportProtocol}")
                    };
                    sinkOptions.Endpoint = options.Endpoint!.ToString();
                    sinkOptions.LogsEndpoint = options.Endpoint!.ToString();
                    sinkOptions.TracesEndpoint = options.Endpoint!.ToString();
                    sinkOptions.ResourceAttributes = new Dictionary<string, object> { ["service.name"] = context.Name };
                });
        }

        return loggerConfiguration;
    }
}
