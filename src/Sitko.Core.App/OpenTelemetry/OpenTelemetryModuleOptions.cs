using OpenTelemetry;
using OpenTelemetry.Exporter;

namespace Sitko.Core.App.OpenTelemetry;

public class OpenTelemetryModuleOptions : BaseModuleOptions
{
    public bool EnableOtlpExport { get; set; }
    public Uri? Endpoint { get; set; }
    public OtlpExportProtocol OtlpExportProtocol { get; set; } = OtlpExportProtocol.Grpc;
    public bool EnableLogs { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
    public bool EnableTracing { get; set; } = true;

    internal List<Action<IApplicationContext, OpenTelemetryModuleOptions, OpenTelemetryBuilder>>
        ConfigureActions { get; } = new();

    internal void Configure(Action<IApplicationContext, OpenTelemetryModuleOptions, OpenTelemetryBuilder> configure) =>
        ConfigureActions.Add(configure);
}
