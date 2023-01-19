using System.Net;
using Consul;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.App;
using Sitko.Core.Consul;
using Sitko.Core.Grpc.Client.Discovery;

namespace Sitko.Core.Grpc.Client.Consul;

public class ConsulGrpcServiceAddressResolver<TClient> : IGrpcServiceAddressResolver<TClient>, IAsyncDisposable
    where TClient : ClientBase<TClient>
{
    private readonly IConsulClientProvider consulClientProvider;
    private readonly CancellationTokenSource cts = new();
    private readonly ILogger<ConsulGrpcServiceAddressResolver<TClient>> logger;
    private readonly IOptionsMonitor<ConsulGrpcClientModuleOptions<TClient>> optionsMonitor;
    private readonly IApplicationContext applicationContext;

    private readonly string serviceName =
        typeof(TClient).BaseType!.GenericTypeArguments.First().DeclaringType!.Name;

    private ulong lastIndex;

    private Task? refreshTask;
    private List<Uri> target = new();

    public ConsulGrpcServiceAddressResolver(IConsulClientProvider consulClientProvider,
        IOptionsMonitor<ConsulGrpcClientModuleOptions<TClient>> optionsMonitor,
        IApplicationContext applicationContext,
        ILogger<ConsulGrpcServiceAddressResolver<TClient>> logger)
    {
        this.consulClientProvider = consulClientProvider;
        this.optionsMonitor = optionsMonitor;
        this.applicationContext = applicationContext;
        this.logger = logger;
    }

    private ConsulGrpcClientModuleOptions<TClient> Options => optionsMonitor.CurrentValue;

    public async ValueTask DisposeAsync()
    {
        cts.Cancel();
        if (refreshTask != null)
        {
            await refreshTask;
        }

        GC.SuppressFinalize(this);
    }

    public async Task InitAsync()
    {
        await LoadTargetAsync();
        refreshTask = StartRefreshTaskAsync();
    }

    public Uri? GetAddress() => target.Any() ? target.OrderBy(_ => Guid.NewGuid()).First() : null;

    public event EventHandler? OnChange;

    private async Task StartRefreshTaskAsync()
    {
        while (!cts.IsCancellationRequested)
        {
            try
            {
                logger.LogDebug("Wait for configuration load");
                await LoadTargetAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in grpc client for {ServiceName} configuration load task: {ErrorText}",
                    serviceName, ex.ToString());
            }
        }

        logger.LogDebug("Stop waiting for configuration");
    }

    private async Task LoadTargetAsync()
    {
        var serviceResponse =
            await consulClientProvider.Client.Catalog.Service(serviceName, "grpc",
                new QueryOptions { WaitIndex = lastIndex }, cts.Token);
        if (serviceResponse.StatusCode == HttpStatusCode.OK)
        {
            lastIndex = serviceResponse.LastIndex;
            if (serviceResponse.Response.Any())
            {
                var services = serviceResponse.Response.Where(catalogService =>
                    !catalogService.ServiceMeta.TryGetValue("Environment", out var env) ||
                    env == applicationContext.Environment).ToList();
                if (!services.Any())
                {
                    logger.LogError("No for services {ServiceName} for environment {Environment}", serviceName,
                        applicationContext.Environment);
                    target = new List<Uri>();
                }

                var serviceUrls = services.Select(service => new Uri(
                        $"{(Options.EnableHttp2UnencryptedSupport ? "http" : "https")}://{service.ServiceAddress}:{service.ServicePort}"))
                    .OrderBy(uri => uri).ToList();

                if (serviceUrls.SequenceEqual(target))
                {
                    return;
                }

                target = serviceUrls;
                logger.LogInformation("Target for {Type} loaded: {Urls}", typeof(TClient),
                    string.Join(", ", serviceUrls));
            }
            else
            {
                logger.LogError("Empty response from consul for service {ServiceName}", serviceName);
                target = new List<Uri>();
            }

            OnChange?.Invoke(this, EventArgs.Empty);
        }
    }
}
