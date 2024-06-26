using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.App;

namespace Sitko.Core.ServiceDiscovery;

public class ServiceDiscoveryModule<TOptions, TProvider> : BaseApplicationModule<TOptions>
    where TOptions : ServiceDiscoveryModuleOptions, new() where TProvider : class, IServiceDiscoveryProvider
{
    public override string OptionsKey => "ServiceDiscovery";

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TOptions startupOptions)
    {
        services.AddSingleton<IServiceDiscoveryProvider, TProvider>();
        services.Configure<ServiceDiscoveryProviderOptions>(_ => { });
        services.AddHostedService<ServiceDiscoveryHostedService>();
        services.AddHostedService<ServiceDiscoveryRefresherService<TOptions>>();
    }
}

public class ServiceDiscoveryHostedService(IServiceDiscoveryProvider provider) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => provider.RegisterAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => provider.UnregisterAsync(cancellationToken);
}

public class ServiceDiscoveryRefresherService<TOptions>(
    IServiceDiscoveryProvider provider,
    IOptionsMonitor<TOptions> hostOptions,
    ILogger<ServiceDiscoveryRefresherService<TOptions>> logger)
    : BackgroundService where TOptions : ServiceDiscoveryModuleOptions, new()
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(hostOptions.CurrentValue.RefreshIntervalInSeconds),
                    stoppingToken);
                await provider.RefreshAsync(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                //do nothing
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error refreshing SD services: {ErrorText}", ex.Message);
            }
        }
    }
}

public interface IServiceDiscoveryProvider
{
    Task RegisterAsync(CancellationToken cancellationToken = default);
    Task UnregisterAsync(CancellationToken cancellationToken = default);
    ResolvedService? Resolve(string type, string name, CancellationToken cancellationToken = default);
    Task RefreshAsync(CancellationToken cancellationToken = default);
    Task LoadAsync();
}

public abstract class BaseServiceDiscoveryProvider(
    IOptionsMonitor<ServiceDiscoveryModuleOptions> hostOptions,
    IOptionsMonitor<ServiceDiscoveryProviderOptions> providerOptions,
    ILogger<BaseServiceDiscoveryProvider> logger)
    : IServiceDiscoveryProvider, IAsyncDisposable
{
    protected ILogger<BaseServiceDiscoveryProvider> Logger { get; } = logger;

    public async Task RegisterAsync(CancellationToken cancellationToken = default) =>
        await DoRegisterAsync(BuildRegistry(), cancellationToken);

    public async Task UnregisterAsync(CancellationToken cancellationToken = default) =>
        await DoUnregisterAsync(BuildRegistry(), cancellationToken);

    public ResolvedService? Resolve(string type, string name,
        CancellationToken cancellationToken = default) =>
        !isLoaded ? null : services.FirstOrDefault(service => service.Type == type && service.Name == name);

    public abstract Task RefreshAsync(CancellationToken cancellationToken = default);

    private bool isInit;
    private bool isLoaded;
    private readonly CancellationTokenSource cts = new();
    private Task? refreshTask;
    private ICollection<ResolvedService> services = Array.Empty<ResolvedService>();

    public async Task LoadAsync()
    {
        if (!isInit)
        {
            isInit = true;
            await LoadServicesAsync();
            refreshTask = StartRefreshTaskAsync();
        }
    }

    private async Task LoadServicesAsync()
    {
        var result = await DoLoadServicesAsync();
        if (result is not null)
        {
            services = result;
            isLoaded = true;
        }
    }

    protected abstract Task<ICollection<ResolvedService>?> DoLoadServicesAsync();

    private async Task StartRefreshTaskAsync()
    {
        while (!cts.IsCancellationRequested)
        {
            try
            {
                logger.LogDebug("Wait for configuration load");
                await LoadServicesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in service discovery load task: {ErrorText}", ex.ToString());
            }
        }

        logger.LogDebug("Stop waiting for configuration");
    }

    private Dictionary<ServiceDiscoveryHost, List<ServiceDiscoveryService>> BuildRegistry()
    {
        var registry = new Dictionary<ServiceDiscoveryHost, List<ServiceDiscoveryService>>();
        foreach (var host in hostOptions.CurrentValue.Hosts)
        {
            registry[host] = new List<ServiceDiscoveryService>();
        }

        foreach (var service in providerOptions.CurrentValue.Services)
        {
            var host = hostOptions.CurrentValue.Hosts.FirstOrDefault(
                discoveryHost => discoveryHost.Type == service.Type);
            if (host is null)
            {
                Logger.LogWarning("Can't find host for service {Service}", service);
                continue;
            }

            registry[host].Add(service);
        }

        return registry;
    }

    protected abstract Task DoRegisterAsync(Dictionary<ServiceDiscoveryHost, List<ServiceDiscoveryService>> registry,
        CancellationToken cancellationToken);

    protected abstract Task DoUnregisterAsync(Dictionary<ServiceDiscoveryHost, List<ServiceDiscoveryService>> registry,
        CancellationToken cancellationToken);

    public async ValueTask DisposeAsync()
    {
        await cts.CancelAsync();
        if (refreshTask != null)
        {
            await refreshTask;
        }

        GC.SuppressFinalize(this);
    }
}

public record ResolvedService(
    string Type,
    string Name,
    Dictionary<string, string> Metadata,
    string Scheme,
    string Host,
    int Port);

[PublicAPI]
public class ServiceDiscoveryModuleOptions : BaseModuleOptions
{
    public List<ServiceDiscoveryHost> Hosts { get; set; } = new();
    public int ChecksIntervalInSeconds { get; set; } = 60;
    public int DeregisterTimeoutInSeconds { get; set; } = 60;
    public int RefreshIntervalInSeconds { get; set; } = 15;
}

[PublicAPI]
public record ServiceDiscoveryHost(string Type, bool Tls, string Address, int Port)
{
}

[PublicAPI]
public class ServiceDiscoveryProviderOptions
{
    private readonly HashSet<ServiceDiscoveryService> services = new();

    internal ICollection<ServiceDiscoveryService> Services => services.ToArray();

    public ServiceDiscoveryProviderOptions RegisterService(ServiceDiscoveryService service)
    {
        services.Add(service);
        return this;
    }

    public ServiceDiscoveryProviderOptions Clear()
    {
        services.Clear();
        return this;
    }
}

[PublicAPI]
public record ServiceDiscoveryService(string Type, string Name, Dictionary<string, string> Metadata);

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddToServiceDiscovery(this IServiceCollection services,
        ServiceDiscoveryService service)
    {
        services.Configure<ServiceDiscoveryProviderOptions>(options =>
        {
            options.RegisterService(service);
        });
        return services;
    }
}
