using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace Sitko.Core.Grpc.Client.Consul
{
    public class ConsulGrpcClientProvider<T> : IGrpcClientProvider<T>, IAsyncDisposable where T : ClientBase<T>
    {
        private readonly ILogger<ConsulGrpcClientProvider<T>> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IConsulClient _consulClient;
        private readonly GrpcClientModuleConfig _config;
        private readonly AsyncLock _locker = new AsyncLock();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private string? _target;
        private ulong _lastIndex;
        private readonly string _serviceName = typeof(T).BaseType!.GenericTypeArguments!.First().DeclaringType!.Name;
        private readonly Task _refreshTask;
        private GrpcChannel? _channel;
        private GrpcClient<T>? _client;

        public ConsulGrpcClientProvider(
            ILogger<ConsulGrpcClientProvider<T>> logger,
            ILoggerFactory loggerFactory,
            IConsulClient consulClient, GrpcClientModuleConfig config)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _consulClient = consulClient;
            _config = config;
            _refreshTask = StartRefreshTaskAsync();
        }


        private async Task<bool> GetChannelAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using (await _locker.LockAsync(cts.Token))
                {
                    if (_channel != null)
                    {
                        return true;
                    }

                    var handler = new HttpClientHandler();


                    if (_config.DisableCertificatesValidation)
                    {
                        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                    }

                    var client = new HttpClient(handler) {BaseAddress = new Uri(_target)};
                    if (_config.EnableHttp2UnencryptedSupport)
                    {
                        AppContext.SetSwitch(
                            "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                    }

                    _channel = GrpcChannel.ForAddress(_target!,
                        new GrpcChannelOptions {HttpClient = client, LoggerFactory = _loggerFactory});
                    try
                    {
                        _logger.LogInformation("Channel {type} connected to {target}", typeof(T), _target);
                        return true;
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogError("Can't connect to grpc server {target} before timeout.", _target);
                        return false;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Can't connect to grpc server {target} with error: {errorText}.", _target,
                            ex.ToString());
                        return false;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw new Exception($"Can't lock for channel of {typeof(T)} creation!");
            }
        }

        public async Task<GrpcClient<T>> GetClientAsync()
        {
            while (_client == null || !_client.IsReady)
            {
                _logger.LogDebug("Client provider {type} wait for configuration", typeof(T));
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            return _client;
        }

        private async Task CreateClientAsync()
        {
            if (_client == null)
            {
                _client = new GrpcClient<T>();
            }
            else
            {
                _client.SetReady(false);
            }

            _logger.LogDebug("Create client of type {type}", typeof(T));
            var channelCreated = await GetChannelAsync();
            if (!channelCreated)
            {
                return;
            }

            _client.SetInstance((T)Activator.CreateInstance(typeof(T), _channel));
            _client.SetReady(true);
        }

        private async Task StartRefreshTaskAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    _logger.LogDebug("Wait for configuration load");
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
                                $"{(_config.EnableHttp2UnencryptedSupport ? "http" : "https")}://{service.ServiceAddress}:{service.ServicePort}";

                            if (target != _target)
                            {
                                _target = target;
                                _logger.LogInformation("Target for {type} loaded: {target}", typeof(T), _target);
                                if (_channel != null)
                                {
                                    _channel.Dispose();
                                    _channel = null;
                                }

                                await CreateClientAsync();
                            }
                        }
                        else
                        {
                            if (_client != null)
                            {
                                _client.SetReady(false);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in grpc client for {serviceName} configuration load task: {errorText}",
                        _serviceName, ex.ToString());
                }
            }

            _logger.LogDebug("Stop waiting for configuration");
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            _cts.Dispose();
            if (_refreshTask != null)
            {
                await _refreshTask;
                _refreshTask.Dispose();
            }
        }
    }
}
