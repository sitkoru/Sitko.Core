using OpenTelemetry;

namespace Sitko.Core.App.OpenTelemetry;

public interface IOpenTelemetryModule : IApplicationModule;

public interface IOpenTelemetryModule<in TModuleOptions> : IOpenTelemetryModule, IApplicationModule<TModuleOptions>
    where TModuleOptions : class, new()
{
    OpenTelemetryBuilder ConfigureOpenTelemetry(IApplicationContext context, TModuleOptions options,
        OpenTelemetryBuilder builder);
}
