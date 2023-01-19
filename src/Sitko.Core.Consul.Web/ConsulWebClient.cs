using System.Net;
using Consul;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.App;

namespace Sitko.Core.Consul.Web;

public class ConsulWebClient
{
    private readonly IOptionsMonitor<ConsulWebModuleOptions> configMonitor;
    private readonly IApplicationContext applicationContext;
    private readonly IConsulClientProvider consulClientProvider;
    private readonly string healthUrl;
    private readonly ILogger<ConsulWebClient> logger;
    private readonly string name;

    private readonly Uri uri;

    public ConsulWebClient(IServer server, IConsulClientProvider consulClientProvider,
        IOptionsMonitor<ConsulWebModuleOptions> config,
        IApplicationContext applicationContext, ILogger<ConsulWebClient> logger)
    {
        this.consulClientProvider = consulClientProvider;
        configMonitor = config;
        this.applicationContext = applicationContext;
        this.logger = logger;

        name = applicationContext.Name;
        if (Options.ServiceUri != null)
        {
            uri = Options.ServiceUri;
        }
        else
        {
            var addressesFeature = server.Features.Get<IServerAddressesFeature>();
            if (addressesFeature is null)
            {
                throw new InvalidOperationException("IServerAddressesFeature not present");
            }

            var addresses = new List<Uri>();
            foreach (var featureAddress in addressesFeature.Addresses)
            {
                var preparedAddress = featureAddress
                    .Replace("localhost", Options.IpAddress)
                    .Replace("0.0.0.0", Options.IpAddress)
                    .Replace("[::]", Options.IpAddress);

                var uriCreated = Uri.TryCreate(preparedAddress, UriKind.Absolute,
                    out var newUri);
                if (uriCreated && newUri != null)
                {
                    addresses.Add(newUri);
                }
                else
                {
                    this.logger.LogWarning("Can't parse address {Address}", featureAddress);
                }
            }

            if (!addresses.Any())
            {
                throw new InvalidOperationException("No addresses available for consul registration");
            }

            var address = addresses.OrderBy(u => u.Port).FirstOrDefault(u => u.Scheme != "https");
            if (address == null)
            {
                address = addresses.First();
            }

            this.logger.LogInformation("Consul uri: {Uri}", address);
            uri = address;
        }

        healthUrl = new Uri(uri, Options.HealthCheckPath).ToString();
    }

    private ConsulWebModuleOptions Options => configMonitor.CurrentValue;

    public async Task RegisterAsync()
    {
        var registration = new AgentServiceRegistration
        {
            ID = name,
            Name = name,
            Address = uri.Host,
            Port = uri.Port,
            Check = new AgentServiceCheck
            {
                HTTP = healthUrl,
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(Options.DeregisterTimeoutInSeconds),
                Interval = TimeSpan.FromSeconds(Options.ChecksIntervalInSeconds)
            },
            Tags = new[] { "metrics" },
            Meta = new Dictionary<string, string>
            {
                { "Environment", applicationContext.Environment },
                { "Instance", applicationContext.Id.ToString() },
                { "Version", applicationContext.Version },
                { "HealthUrl", healthUrl },
            }
        };

        logger.LogInformation("Registering in Consul as {Name} on {Host}:{Port}", name,
            uri.Host, uri.Port);
        await consulClientProvider.Client.Agent.ServiceDeregister(registration.ID);
        var result = await consulClientProvider.Client.Agent.ServiceRegister(registration);
        logger.LogInformation("Consul response code: {Code}", result.StatusCode);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        CancellationToken cancellationToken = new())
    {
        var serviceResponse = await consulClientProvider.Client.Catalog.Service(name, "metrics", cancellationToken);
        if (serviceResponse.StatusCode == HttpStatusCode.OK)
        {
            if (serviceResponse.Response.Any())
            {
                var services = serviceResponse.Response.Where(catalogService =>
                    catalogService.ServiceMeta.TryGetValue("Environment", out var env) ||
                    env == applicationContext.Environment).ToList();
                if (services.Any())
                {
                    return HealthCheckResult.Healthy();
                }
            }

            if (Options.AutoFixRegistration)
            {
                //no services. fix registration
                await RegisterAsync();
            }

            return HealthCheckResult.Degraded($"No service registered with name {name}");
        }

        return HealthCheckResult.Unhealthy($"Error response from consul: {serviceResponse.StatusCode}");
    }
}
