using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery;
using Serilog;
using Serilog.Events;

namespace Sitko.Core.ServiceDiscovery;

public interface IServiceDiscoveryModule;

public class ServiceDiscoveryModule : BaseApplicationModule<ServiceDiscoveryModuleOptions>, IServiceDiscoveryModule,
    ILoggingModule<ServiceDiscoveryModuleOptions>
{
    public override string OptionsKey => "ServiceDiscovery";

    public LoggerConfiguration ConfigureLogging(IApplicationContext context, ServiceDiscoveryModuleOptions options,
        LoggerConfiguration loggerConfiguration) => loggerConfiguration.Filter.ByExcluding(static logEvent =>
        // shortcut: причины отмены неразличимы; пересмотреть после https://github.com/dotnet/extensions/issues/7940
        logEvent.Exception is OperationCanceledException &&
        logEvent.MessageTemplate.Text == "Error resolving endpoints for service '{ServiceName}'." &&
        logEvent.Properties.TryGetValue("SourceContext", out var source) &&
        source is ScalarValue { Value: "Microsoft.Extensions.ServiceDiscovery.ServiceEndpointWatcher" } &&
        logEvent.Properties.TryGetValue("EventId", out var eventId) &&
        eventId is StructureValue eventIdStructure &&
        eventIdStructure.Properties.Any(static property => property is { Name: "Id", Value: ScalarValue { Value: 4 } }) &&
        eventIdStructure.Properties.Any(static property => property is { Name: "Name", Value: ScalarValue { Value: "ResolutionFailed" } }));

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        ServiceDiscoveryModuleOptions startupOptions) => services.AddServiceDiscoveryCore();

    public override Task InitAsync(IApplicationContext applicationContext, IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var declarations = serviceProvider.GetRequiredService<IOptions<ServiceDiscoveryOptions>>().Value.Services;
        if (declarations.Count > 0 && !serviceProvider.GetServices<IServiceEndpointProviderFactory>().Any())
        {
            throw new InvalidOperationException($"Contracts {string.Join(", ", declarations.Select(static service => service.Name))} require a ServiceDiscovery provider for publication");
        }
        return Task.CompletedTask;
    }
}

public class ServiceDiscoveryModuleOptions : BaseModuleOptions
{
}
