using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.ServiceDiscovery;

public abstract class BaseServiceDiscoveryResolver(
    ILogger<BaseServiceDiscoveryResolver> logger)
    : IServiceDiscoveryResolver, IAsyncDisposable
{
    private readonly CancellationTokenSource loadCancellationTokenSource = new();

    private readonly ConcurrentDictionary<string, List<Action<ResolvedService[]>>> resolveCallbacks = new();

    private bool isInit;
    private bool isLoaded;
    private Task? refreshTask;
    private Dictionary<string, ResolvedService[]> services = new();
    protected ILogger<BaseServiceDiscoveryResolver> Logger { get; } = logger;

    public async ValueTask DisposeAsync()
    {
        await loadCancellationTokenSource.CancelAsync();
        if (refreshTask != null)
        {
            await refreshTask;
        }

        GC.SuppressFinalize(this);
    }

    public ResolvedService[]? Resolve(string type, string name)
    {
        var key = GenerateServiceKey(type, name);
        if (isLoaded && services.TryGetValue(key, out var resolvedServices))
        {
            return resolvedServices;
        }

        return null;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!isInit)
        {
            using var cts =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, loadCancellationTokenSource.Token);
            isInit = true;
            await LoadServicesAsync(cts.Token);
            refreshTask = StartRefreshTaskAsync();
        }
    }

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

    private static string GenerateServiceKey(string serviceType, string name) =>
        $"{serviceType}|{name}".ToLowerInvariant();

    private static string GenerateServiceKey(ResolvedService service) => GenerateServiceKey(service.Type, service.Name);

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
}
