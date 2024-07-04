using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.ServiceDiscovery;

public abstract class BaseServiceDiscoveryResolver(
    ILogger<BaseServiceDiscoveryResolver> logger)
    : IServiceDiscoveryResolver, IAsyncDisposable
{
    protected ILogger<BaseServiceDiscoveryResolver> Logger { get; } = logger;

    public ResolvedService[]? Resolve(string type, string name)
    {
        var key = GenerateServiceKey(type, name);
        if (isLoaded && services.TryGetValue(key, out var resolvedServices))
        {
            return resolvedServices;
        }

        return null;
    }

    private bool isInit;
    private bool isLoaded;
    private readonly CancellationTokenSource loadCancellationTokenSource = new();
    private Task? refreshTask;
    private Dictionary<string, ResolvedService[]> services = new();

    public async Task LoadAsync()
    {
        if (!isInit)
        {
            isInit = true;
            await LoadServicesAsync(loadCancellationTokenSource.Token);
            refreshTask = StartRefreshTaskAsync();
        }
    }

    private readonly ConcurrentDictionary<string, List<Action<ResolvedService[]>>> resolveCallbacks = new();

    private static string GenerateServiceKey(string serviceType, string name) =>
        $"{serviceType}|{name}".ToLowerInvariant();

    private static string GenerateServiceKey(ResolvedService service) => GenerateServiceKey(service.Type, service.Name);

    public void Subscribe(string serviceType, string name, Action<ResolvedService[]> callback)
    {
        var key = GenerateServiceKey(serviceType, name);
        resolveCallbacks.AddOrUpdate(key, _ => [callback],
            (_, list) =>
            {
                list.Add(callback);
                return list;
            });
        UpdateSubscriptions();
    }

    private async Task LoadServicesAsync(CancellationToken cancellationToken)
    {
        var result = await DoLoadServicesAsync(cancellationToken);
        if (result is not null)
        {
            var newServices = new Dictionary<string, ResolvedService[]>();
            foreach (var servicesGroup in result.GroupBy(GenerateServiceKey))
            {
                newServices[servicesGroup.Key] = servicesGroup.ToArray();
            }

            services = newServices;
            isLoaded = true;
            UpdateSubscriptions();
        }
    }

    private void UpdateSubscriptions()
    {
        foreach (var (key, service) in services)
        {
            if (resolveCallbacks.TryGetValue(key, out var serviceCallbacks))
            {
                foreach (var serviceCallback in serviceCallbacks)
                {
                    serviceCallback(service);
                }
            }
        }
    }

    protected abstract Task<ICollection<ResolvedService>?> DoLoadServicesAsync(CancellationToken cancellationToken);

    private async Task StartRefreshTaskAsync()
    {
        while (!loadCancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                Logger.LogDebug("Wait for configuration load");
                await LoadServicesAsync(loadCancellationTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                Logger.LogInformation("Service discovery load task was cancelled");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in service discovery load task: {ErrorText}", ex.ToString());
            }
        }

        Logger.LogDebug("Stop waiting for configuration");
    }

    public async ValueTask DisposeAsync()
    {
        await loadCancellationTokenSource.CancelAsync();
        if (refreshTask != null)
        {
            await refreshTask;
        }

        GC.SuppressFinalize(this);
    }
}
