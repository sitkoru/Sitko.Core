using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using Sitko.Core.App;
using Sitko.Core.App.OpenTelemetry;

namespace Sitko.Core.ClickHouse;

public class
    ClickHouseModule : BaseApplicationModule<ClickHouseModuleOptions>,
    IOpenTelemetryModule<ClickHouseModuleOptions>
{
    public override string OptionsKey => "ClickHouse";

    public OpenTelemetryBuilder ConfigureOpenTelemetry(IApplicationContext context,
        ClickHouseModuleOptions options,
        OpenTelemetryBuilder builder) =>
        builder.WithTracing(providerBuilder => providerBuilder.AddSource("ClickHouse.Client"));

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        ClickHouseModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddScoped<IClickHouseDbProvider, ClickHouseDbProvider>();
    }
}
