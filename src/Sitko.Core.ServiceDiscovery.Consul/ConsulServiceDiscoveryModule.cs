using System.Collections.Concurrent;
using System.Net;
using Consul;
using Consul.Filtering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.App;
using Sitko.Core.Consul;

namespace Sitko.Core.ServiceDiscovery.Consul;

public class ConsulServiceDiscoveryModule : ServiceDiscoveryModule<ConsulServiceDiscoveryModuleOptions,
    ConsulServiceDiscoveryProvider>
{
    public override IEnumerable<Type> GetRequiredModules(IApplicationContext applicationContext,
        ConsulServiceDiscoveryModuleOptions options) => [typeof(ConsulModule)];
}

public class ConsulServiceDiscoveryProvider(
    IConsulClientProvider consulClientProvider,
    IApplicationContext applicationContext,
    IOptionsMonitor<ConsulServiceDiscoveryModuleOptions> hostOptions,
    IOptionsMonitor<ServiceDiscoveryProviderOptions> providerOptions,
    ILogger<ConsulServiceDiscoveryProvider> logger) : BaseServiceDiscoveryProvider(hostOptions, providerOptions, logger)
{
    private readonly ConcurrentDictionary<string, string> registeredServices = new();
    private ulong lastIndex;
    private readonly CancellationTokenSource cts = new();

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Update TTL for gRPC services");
        foreach (var service in registeredServices)
        {
            logger.LogDebug("Service: {ServiceId}/{ServiceName}", service.Key, service.Value);
            try
            {
                await consulClientProvider.Client.Agent.UpdateTTL("service:" + service.Key,
                    $"Last update: {DateTime.UtcNow:O}", TTLStatus.Pass,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error updating TTL for {ServiceId}/{ServiceName}: {ErrorText}",
                    service.Key, service.Value, exception.ToString());
            }
        }

        logger.LogDebug("All gRPC services TTL updated");
    }

    protected override async Task<ICollection<ResolvedService>?> DoLoadServicesAsync()
    {
        var filter = new ServiceTagsSelector().Contains("sd:true");
        var request = new GetRequest<Dictionary<string, string[]>>((ConsulClient)consulClientProvider.Client,
            "/v1/catalog/services",
            new QueryOptions { WaitIndex = lastIndex }, filter);
        var serviceResponse =
            await request.Execute(cts.Token);
        if (serviceResponse.StatusCode == HttpStatusCode.OK)
        {
            lastIndex = serviceResponse.LastIndex;
            if (serviceResponse.Response.Count != 0)
            {
                var resolvedServices = new List<ResolvedService>();
                foreach (var (serviceName, tags) in serviceResponse.Response)
                {
                    var serviceInfoResponse = await consulClientProvider.Client.Catalog.Service(serviceName, cts.Token);
                    if (serviceInfoResponse.StatusCode == HttpStatusCode.OK)
                    {
                        var services = serviceInfoResponse.Response.Where(catalogService =>
                            !catalogService.ServiceMeta.TryGetValue("Environment", out var env) ||
                            env == applicationContext.Environment).ToList();
                        if (services.Count != 0)
                        {
                            string? type = null;
                            string? scheme = null;
                            var serviceNames = new List<string>();
                            foreach (var tag in tags)
                            {
                                if (type is null && tag.StartsWith("sdType", StringComparison.OrdinalIgnoreCase))
                                {
                                    type = tag.Replace("sdType:", "", StringComparison.OrdinalIgnoreCase);
                                }
                                else if (scheme is null &&
                                         tag.StartsWith("sdScheme", StringComparison.OrdinalIgnoreCase))
                                {
                                    scheme = tag.Replace("sdScheme:", "", StringComparison.OrdinalIgnoreCase);
                                }
                                else
                                {
                                    if (tag.StartsWith("service:", StringComparison.OrdinalIgnoreCase))
                                    {
                                        serviceNames.Add(
                                            tag.Replace("service:", "", StringComparison.OrdinalIgnoreCase));
                                    }
                                }
                            }

                            if (type is null || scheme is null)
                            {
                                continue;
                            }

                            foreach (var service in services)
                            {
                                foreach (var applicationServiceName in serviceNames)
                                {
                                    var resolvedService = new ResolvedService(type, applicationServiceName,
                                        new Dictionary<string, string>(), scheme, service.Address, service.ServicePort);
                                    resolvedServices.Add(resolvedService);
                                }
                            }
                        }
                    }
                }

                return resolvedServices;
            }
        }

        return null;
    }

    protected override async Task DoRegisterAsync(
        Dictionary<ServiceDiscoveryHost, List<ServiceDiscoveryService>> registry,
        CancellationToken cancellationToken)
    {
        foreach (var (host, services) in registry)
        {
            var serviceName = $"{applicationContext.Name}_{host.Type}";
            var meta = new Dictionary<string, string>
            {
                { "ServiceType", host.Type },
                { "Environment", applicationContext.Environment },
                { "Version", applicationContext.Version }
            };
            var tags = new List<string>
            {
                "sd:true", $"sdType:{host.Type}", $"sdScheme:{(host.Tls ? "https" : "http")}"
            };
            foreach (var service in services)
            {
                tags.Add($"service:{service.Name}");
                foreach (var (metaKey, metaValue) in service.Metadata)
                {
                    meta.Add($"meta-{service.Name}-{metaKey}", metaValue);
                }
            }

            var registration = new AgentServiceRegistration
            {
                ID = serviceName,
                Name = serviceName,
                Address = host.Address,
                Port = host.Port,
                Check = new AgentServiceCheck
                {
                    TTL = TimeSpan.FromSeconds(hostOptions.CurrentValue.ChecksIntervalInSeconds),
                    DeregisterCriticalServiceAfter =
                        TimeSpan.FromSeconds(hostOptions.CurrentValue.DeregisterTimeoutInSeconds)
                },
                Tags = tags.ToArray(),
                Meta = meta
            };
            Logger.LogInformation("Register grpc service {ServiceName} on {Address}:{Port}", serviceName, host.Address,
                host.Port);
            await consulClientProvider.Client.Agent.ServiceDeregister(serviceName, cancellationToken);
            var result = await consulClientProvider.Client.Agent.ServiceRegister(registration, cancellationToken);
            Logger.LogInformation("Consul response code: {Code}", result.StatusCode);

            registeredServices.TryAdd(serviceName, serviceName);
        }
    }

    protected override Task DoUnregisterAsync(Dictionary<ServiceDiscoveryHost, List<ServiceDiscoveryService>> registry,
        CancellationToken cancellationToken) =>
        // do nothing
        Task.CompletedTask;
}

public class ConsulServiceDiscoveryModuleOptions : ServiceDiscoveryModuleOptions
{
}

public sealed class ServiceTagsSelector :
    Selector
{
    private static readonly string Self = "ServiceTags";

    public override string Encode() => Selector.Combine("", ServiceTagsSelector.Self);

    public Filter Contains(string value) => new ContainsFilter<ServiceTagsSelector> { Selector = this, Value = value };
}

internal sealed class ContainsFilter<TSelector> : Filter where TSelector : Selector
{
    public TSelector Selector { get; set; }

    public string Value { get; set; }

    public override string Encode() => Selector.Encode() + " contains " + Quote(Value);
}
