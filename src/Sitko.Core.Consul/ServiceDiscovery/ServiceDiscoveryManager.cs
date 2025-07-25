using System.Net;
using System.Text;
using System.Text.Json;
using Consul;
using Consul.Filtering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.App;
using Sitko.Core.App.Helpers;
using Sitko.Core.ServiceDiscovery;

namespace Sitko.Core.Consul.ServiceDiscovery;

public interface IServiceDiscoveryManager
{
    Task<(string Id, string ServiceName)> RegisterAsync(ApplicationService applicationService,
        List<ServiceDiscoveryService> services,
        CancellationToken cancellationToken = default);

    Task<ICollection<ResolvedService>?> LoadAsync(CancellationToken cancellationToken = default);

    Task RefreshTtlAsync(string serviceId, CancellationToken cancellationToken = default);
    Task DeregisterAsync(string serviceId, CancellationToken cancellationToken);
}

public class ServiceDiscoveryManager(
    IApplicationContext applicationContext,
    IConsulClientProvider consulClientProvider,
    IOptionsMonitor<ConsulModuleOptions> options,
    ILogger<ServiceDiscoveryManager> logger)
    : IServiceDiscoveryManager
{
    private const string EnvironmentKey = "Environment";
    private const string VersionKey = "Version";
    private const string ServiceInfoKey = "ServiceInfo";
    private const string ServicesListKey = "ServicesList";
    private const string ServiceDiscoveryEnabledTag = "sd:true";
    private const string SchemeTag = "scheme";
    private const string ServiceTag = "service";

    private ulong lastIndex;

    public async Task<(string Id, string ServiceName)> RegisterAsync(ApplicationService applicationService,
        List<ServiceDiscoveryService> services, CancellationToken cancellationToken)
    {
        var serviceName = $"{applicationContext.Name}_{applicationService.Name}";
        var serviceId =
            $"{applicationService.Name}_{applicationService.Address}_{applicationService.Port}_{(DockerHelper.IsRunningInDocker() ? Environment.MachineName : applicationContext.Id)}";
        var meta = BuildMetadata(applicationService, services);
        meta.Add(EnvironmentKey, applicationContext.Environment);
        meta.Add(VersionKey, applicationContext.Version);
        var tags = new List<string>
        {
            ServiceDiscoveryEnabledTag,
            $"{SchemeTag}:{applicationService.Scheme}",
            $"{EnvironmentKey}:{applicationContext.Environment}"
        };
        tags.AddRange(services.Select(service => $"{ServiceTag}:{service.Name}"));

        var registration = new AgentServiceRegistration
        {
            ID = serviceId,
            Name = serviceName,
            Address = applicationService.Address,
            Port = applicationService.Port,
            Check = new AgentServiceCheck
            {
                TTL = TimeSpan.FromSeconds(options.CurrentValue.ChecksIntervalInSeconds),
                DeregisterCriticalServiceAfter =
                    TimeSpan.FromSeconds(options.CurrentValue.DeregisterTimeoutInSeconds)
            },
            Tags = tags.ToArray(),
            Meta = meta
        };

        await consulClientProvider.Client.Agent.ServiceDeregister(serviceId, cancellationToken);
        var result = await consulClientProvider.Client.Agent.ServiceRegister(registration, cancellationToken);
        logger.LogInformation("Consul response code: {Code}", result.StatusCode);
        return (serviceId, serviceName);
    }

    public async Task<ICollection<ResolvedService>?> LoadAsync(CancellationToken cancellationToken = default)
    {
        var filter = new ServiceTagsSelector().Contains(ServiceDiscoveryEnabledTag);
        var request = new GetRequest<Dictionary<string, string[]>>((ConsulClient)consulClientProvider.Client,
            "/v1/catalog/services",
            new QueryOptions { WaitIndex = lastIndex }, filter);
        var serviceResponse =
            await request.Execute(cancellationToken);
        if (serviceResponse.StatusCode == HttpStatusCode.OK)
        {
            logger.LogDebug("Received services list from Consul");
            lastIndex = serviceResponse.LastIndex;
            if (serviceResponse.Response.Count != 0)
            {
                var resolvedServices = new List<ResolvedService>();
                foreach (var (serviceName, _) in serviceResponse.Response)
                {
                    logger.LogDebug("Request service {ServiceName} info", serviceName);
                    var serviceInfoResponse =
                        await consulClientProvider.Client.Catalog.Service(serviceName, cancellationToken);
                    if (serviceInfoResponse.StatusCode == HttpStatusCode.OK)
                    {
                        logger.LogDebug("Received service {ServiceName} info", serviceName);
                        var services = serviceInfoResponse.Response.Where(catalogService =>
                            !catalogService.ServiceMeta.TryGetValue(EnvironmentKey, out var env) ||
                            env == applicationContext.Environment).ToList();
                        foreach (var service in services)
                        {
                            var meta = ReadMetadata(service.ServiceMeta);
                            foreach (var sdService in meta.Services)
                            {
                                logger.LogDebug("Resolved service {ServiceName} ({ServiceType}) with address {Address}",
                                    sdService.Name, sdService.Type,
                                    $"{meta.ApplicationService.Scheme}://{service.ServiceAddress}:{service.ServicePort}");
                                var resolvedService = new ResolvedService(sdService.Type, sdService.Name,
                                    new Dictionary<string, string>(), meta.ApplicationService.Scheme,
                                    service.ServiceAddress,
                                    service.ServicePort);
                                resolvedServices.Add(resolvedService);
                            }
                        }
                    }
                }

                return resolvedServices;
            }
        }

        return null;
    }

    public async Task RefreshTtlAsync(string serviceId, CancellationToken cancellationToken)
    {
        try
        {
            await consulClientProvider.Client.Agent.UpdateTTL("service:" + serviceId,
                $"Last update: {DateTime.UtcNow:O}", TTLStatus.Pass,
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error updating TTL for {ServiceId}: {ErrorText}",
                serviceId, exception.ToString());
        }
    }

    public async Task DeregisterAsync(string serviceId, CancellationToken cancellationToken)
    {
        try
        {
            await consulClientProvider.Client.Agent.ServiceDeregister(serviceId, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error deregistering service {ServiceId}: {ErrorText}",
                serviceId, exception.ToString());
        }
    }

    private static Dictionary<string, string> BuildMetadata(ApplicationService applicationService,
        List<ServiceDiscoveryService> services)
    {
        var meta = new Dictionary<string, string>
        {
            { ServiceInfoKey, SerializeToBase64(applicationService) },
            { ServicesListKey, SerializeToBase64(services.Select(service => service.Name).ToArray()) }
        };
        foreach (var service in services)
        {
            meta.Add(service.Name, SerializeToBase64(service));
        }

        return meta;
    }

    private static (ApplicationService ApplicationService,
        List<ServiceDiscoveryService> Services) ReadMetadata(IDictionary<string, string> meta)
    {
        if (!meta.TryGetValue(ServiceInfoKey, out var serviceInfoData))
        {
            throw new InvalidOperationException("No service info header");
        }

        var appService = DeserializeFromBase64<ApplicationService>(serviceInfoData);
        if (appService is null)
        {
            throw new InvalidOperationException("Can't parse meta for app service");
        }

        if (!meta.TryGetValue(ServicesListKey, out var serviceNamesListData))
        {
            throw new InvalidOperationException("No service list header");
        }

        var serviceNamesList = DeserializeFromBase64<string[]>(serviceNamesListData);
        if (serviceNamesList is null || serviceNamesList.Length == 0)
        {
            throw new InvalidOperationException("Empty service names list");
        }

        var services = new List<ServiceDiscoveryService>();
        foreach (var serviceName in serviceNamesList)
        {
            if (!meta.TryGetValue(serviceName, out var serviceData))
            {
                throw new InvalidOperationException($"No meta for service {serviceName}");
            }

            var service = DeserializeFromBase64<ServiceDiscoveryService>(serviceData);
            if (service is null)
            {
                throw new InvalidOperationException($"Can't parse meta for service {serviceName}");
            }

            services.Add(service);
        }

        return (appService, services);
    }

    private static string SerializeToBase64(object data) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data)));

    private static TData? DeserializeFromBase64<TData>(string base64) =>
        JsonSerializer.Deserialize<TData>(Encoding.UTF8.GetString(Convert.FromBase64String(base64)));
}

public sealed class ServiceTagsSelector : Selector
{
    private static readonly string Self = "ServiceTags";

    public override string Encode() => Combine("", Self);

    public Filter Contains(string value) => new ContainsFilter<ServiceTagsSelector>(this, value);
}

internal sealed class ContainsFilter<TSelector>(TSelector selector, string value) : Filter where TSelector : Selector
{
    public override string Encode() => selector.Encode() + " contains " + Quote(value);
}
