using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.Grpc.Client.Discovery;

namespace Sitko.Core.Grpc.Client.Consul
{
    public class ConsulGrpcServiceAddressResolver<TClient> : IGrpcServiceAddressResolver<TClient>, IAsyncDisposable
        where TClient : ClientBase<TClient>
    {
        private readonly IConsulClient _consulClient;
        private readonly IOptionsMonitor<ConsulGrpcClientModuleOptions> _optionsMonitor;
        private ConsulGrpcClientModuleOptions Options => _optionsMonitor.CurrentValue;
        private readonly ILogger<ConsulGrpcServiceAddressResolver<TClient>> _logger;
        private readonly CancellationTokenSource _cts = new();
        private Uri? _target;
        private ulong _lastIndex;

        private readonly string _serviceName =
            typeof(TClient).BaseType!.GenericTypeArguments!.First().DeclaringType!.Name;

        private Task? _refreshTask;

        public ConsulGrpcServiceAddressResolver(IConsulClient consulClient,
            IOptionsMonitor<ConsulGrpcClientModuleOptions> optionsMonitor,
            ILogger<ConsulGrpcServiceAddressResolver<TClient>> logger)
        {
            _consulClient = consulClient;
            _optionsMonitor = optionsMonitor;
            _logger = logger;
        }

        public async Task InitAsync()
        {
            await LoadTargetAsync();
            _refreshTask = StartRefreshTaskAsync();
        }

        private async Task StartRefreshTaskAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    _logger.LogDebug("Wait for configuration load");
                    await LoadTargetAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in grpc client for {ServiceName} configuration load task: {ErrorText}",
                        _serviceName, ex.ToString());
                }
            }

            _logger.LogDebug("Stop waiting for configuration");
        }

        private async Task LoadTargetAsync()
        {
            var serviceResponse =
                await _consulClient.Catalog.Service(_serviceName, "grpc",
                    new QueryOptions {WaitIndex = _lastIndex}, _cts.Token);
            if (serviceResponse.StatusCode == HttpStatusCode.OK)
            {
                _lastIndex = serviceResponse.LastIndex;
                if (serviceResponse.Response.Any())
                {
                    var service = serviceResponse.Response.First();
                    var target =
                        new Uri(
                            $"{(Options.EnableHttp2UnencryptedSupport ? "http" : "https")}://{service.ServiceAddress}:{service.ServicePort}");

                    if (target == _target) return;
                    _target = target;
                    _logger.LogInformation("Target for {Type} loaded: {Target}", typeof(TClient), _target);
                }
                else
                {
                    _logger.LogError("Empty response from consul for service {ServiceName}", _serviceName);
                    _target = null;
                }

                OnChange?.Invoke(this, EventArgs.Empty);
            }
        }

        public Uri? GetAddress()
        {
            return _target;
        }

        public event EventHandler? OnChange;

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            if (_refreshTask != null)
            {
                await _refreshTask;
            }
        }
    }
}
