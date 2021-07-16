namespace Sitko.Core.Grpc.Client.Consul
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Discovery;
    using global::Consul;
    using global::Grpc.Core;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class ConsulGrpcServiceAddressResolver<TClient> : IGrpcServiceAddressResolver<TClient>, IAsyncDisposable
        where TClient : ClientBase<TClient>
    {
        private readonly IConsulClient consulClient;
        private readonly CancellationTokenSource cts = new();
        private readonly ILogger<ConsulGrpcServiceAddressResolver<TClient>> logger;
        private readonly IOptionsMonitor<ConsulGrpcClientModuleOptions> optionsMonitor;

        private readonly string serviceName =
            typeof(TClient).BaseType!.GenericTypeArguments!.First().DeclaringType!.Name;

        private ulong lastIndex;

        private Task? refreshTask;
        private Uri? target;

        public ConsulGrpcServiceAddressResolver(IConsulClient consulClient,
            IOptionsMonitor<ConsulGrpcClientModuleOptions> optionsMonitor,
            ILogger<ConsulGrpcServiceAddressResolver<TClient>> logger)
        {
            this.consulClient = consulClient;
            this.optionsMonitor = optionsMonitor;
            this.logger = logger;
        }

        private ConsulGrpcClientModuleOptions Options => optionsMonitor.CurrentValue;

        public async ValueTask DisposeAsync()
        {
            cts.Cancel();
            if (refreshTask != null)
            {
                await refreshTask;
            }
        }

        public async Task InitAsync()
        {
            await LoadTargetAsync();
            refreshTask = StartRefreshTaskAsync();
        }

        public Uri? GetAddress() => target;

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
                await consulClient.Catalog.Service(serviceName, "grpc",
                    new QueryOptions {WaitIndex = lastIndex}, cts.Token);
            if (serviceResponse.StatusCode == HttpStatusCode.OK)
            {
                lastIndex = serviceResponse.LastIndex;
                if (serviceResponse.Response.Any())
                {
                    var service = serviceResponse.Response.First();
                    var serviceUrl =
                        new Uri(
                            $"{(Options.EnableHttp2UnencryptedSupport ? "http" : "https")}://{service.ServiceAddress}:{service.ServicePort}");

                    if (serviceUrl == target)
                    {
                        return;
                    }

                    target = serviceUrl;
                    logger.LogInformation("Target for {Type} loaded: {Target}", typeof(TClient), this.target);
                }
                else
                {
                    logger.LogError("Empty response from consul for service {ServiceName}", serviceName);
                    target = null;
                }

                OnChange?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
