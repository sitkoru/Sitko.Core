using System;
using System.Collections.Generic;
using System.Net.Http;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.Grpc.Client.Discovery;

namespace Sitko.Core.Grpc.Client;

public interface IGrpcClientProvider<out TClient> where TClient : ClientBase<TClient>
{
    TClient Instance { get; }
    Uri? CurrentAddress { get; }
}

public class GrpcClientProvider<TClient, TOptions> : IGrpcClientProvider<TClient>
    where TClient : ClientBase<TClient> where TOptions : GrpcClientModuleOptions<TClient>, new()
{
    private readonly ILogger<GrpcClientProvider<TClient, TOptions>> logger;
    private readonly ILoggerFactory loggerFactory;

    private readonly IOptionsMonitor<TOptions> optionsMonitor;

    private readonly IGrpcServiceAddressResolver<TClient> resolver;

    private readonly IServiceScopeFactory scopeFactory;

    private CallInvoker? callInvoker;
    private GrpcChannel? channel;

    public GrpcClientProvider(IGrpcServiceAddressResolver<TClient> resolver, IOptionsMonitor<TOptions> optionsMonitor,
        ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory,
        ILogger<GrpcClientProvider<TClient, TOptions>> logger)
    {
        this.resolver = resolver;
        this.optionsMonitor = optionsMonitor;
        this.loggerFactory = loggerFactory;
        this.scopeFactory = scopeFactory;
        this.logger = logger;
        this.resolver.OnChange += (_, _) => OnChange();
        CurrentAddress = this.resolver.GetAddress();
    }

    private TOptions Options => optionsMonitor.CurrentValue;

    public TClient Instance
    {
        get
        {
            if (Activator.CreateInstance(typeof(TClient), GetOrCreateCallInvoker()) is not TClient client)
            {
                throw new InvalidOperationException($"Can't create client of type {typeof(TClient)}");
            }

            return client;
        }
    }

    public Uri? CurrentAddress { get; private set; }

    private CallInvoker GetOrCreateCallInvoker()
    {
        if (callInvoker is not null)
        {
            return callInvoker;
        }

        if (CurrentAddress is null)
        {
            throw new InvalidOperationException($"No address for service {typeof(TClient)}");
        }

        logger.LogInformation("Create new channel for client {Client} to {Address}", typeof(TClient), CurrentAddress);
        var scope = scopeFactory.CreateScope();
        var services = scope.ServiceProvider;
        var options = new GrpcChannelOptions();

        var handler = new HttpClientHandler();
        if (Options.DisableCertificatesValidation)
        {
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        }

        options.HttpHandler = Options.ConfigureHttpHandler?.Invoke(handler) ?? handler;

        options.ServiceProvider = services;
        options.LoggerFactory = loggerFactory;
        Options.ConfigureChannelOptions?.Invoke(options);

        channel = GrpcChannel.ForAddress(CurrentAddress, options);
        var httpClientCallInvoker = channel.CreateCallInvoker();
        var interceptors = new List<Interceptor>();
        foreach (var interceptorType in Options.Interceptors)
        {
            var interceptorInstance = services.GetService(interceptorType);
            if (interceptorInstance is Interceptor interceptor)
            {
                interceptors.Add(interceptor);
            }
        }

        callInvoker = httpClientCallInvoker;
        if (interceptors.Count > 0)
        {
            callInvoker = httpClientCallInvoker.Intercept(interceptors.ToArray());
        }

        return callInvoker;
    }

    private void OnChange()
    {
        var newAddress = resolver.GetAddress();
        logger.LogInformation("Address for client {Client} changed from {OldAddress} to {NewAddress}", typeof(TClient),
            CurrentAddress, newAddress);
        CurrentAddress = newAddress;
        channel?.Dispose();
        callInvoker = null;
    }
}
