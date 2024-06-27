using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.ServiceDiscovery;

public abstract class BaseServiceDiscoveryResolver(
    ILogger<BaseServiceDiscoveryResolver> logger)
    : IServiceDiscoveryResolver, IAsyncDisposable
{
    protected ILogger<BaseServiceDiscoveryResolver> Logger { get; } = logger;

    public ResolvedService? Resolve(string type, string name) =>
        !isLoaded
            ? null
            : services.FirstOrDefault(service =>
                service.Type.Equals(type, StringComparison.OrdinalIgnoreCase) &&
                service.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

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

    private readonly ConcurrentDictionary<string, List<Action<ResolvedService>>> resolveCallbacks = new();

    public void Subscribe(string serviceType, string name, Action<ResolvedService> callback)
    {
        var key = $"{serviceType}|{name}".ToLowerInvariant();
        resolveCallbacks.AddOrUpdate(key, _ => [callback],
            (_, list) =>
            {
                list.Add(callback);
                return list;
            });
    }

    private async Task LoadServicesAsync()
    {
        var result = await DoLoadServicesAsync();
        if (result is not null)
        {
            services = result;
            isLoaded = true;
            foreach (var resolvedService in result)
            {
                var key = $"{resolvedService.Type}|{resolvedService.Name}".ToLowerInvariant();
                if (resolveCallbacks.TryGetValue(key, out var callbacks))
                {
                    foreach (var callback in callbacks)
                    {
                        callback(resolvedService);
                    }
                }
            }
        }
    }

    protected abstract Task<ICollection<ResolvedService>?> DoLoadServicesAsync();

    private async Task StartRefreshTaskAsync()
    {
        while (!cts.IsCancellationRequested)
        {
            try
            {
                Logger.LogDebug("Wait for configuration load");
                await LoadServicesAsync();
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
        await cts.CancelAsync();
        if (refreshTask != null)
        {
            await refreshTask;
        }

        GC.SuppressFinalize(this);
    }
}
