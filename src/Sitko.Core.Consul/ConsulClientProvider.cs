using Consul;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sitko.Core.Consul;

public interface IConsulClientProvider
{
    IConsulClient Client { get; }
}

public class ConsulClientProvider : IConsulClientProvider, IDisposable
{
    private readonly ILogger<ConsulClientProvider> logger;
    private IConsulClient? consulClient;
    private string? lastKnownAddress;

    public ConsulClientProvider(IOptionsMonitor<ConsulModuleOptions> optionsMonitor,
        ILogger<ConsulClientProvider> logger)
    {
        this.logger = logger;
        CreateClient(optionsMonitor.CurrentValue);
        optionsMonitor.OnChange(CreateClient);
    }

    public IConsulClient Client =>
        consulClient ?? throw new InvalidOperationException("Consul client is not initialized");

    public void Dispose()
    {
        consulClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void CreateClient(ConsulModuleOptions options)
    {
        if (consulClient is not null)
        {
            if (lastKnownAddress == options.ConsulUri)
            {
                return;
            }

            consulClient.Dispose();
        }

        logger.LogInformation("Create new consul client with address {ConsulAddress}", options.ConsulUri);
        consulClient = new ConsulClient(configuration =>
        {
            configuration.Address = new Uri(options.ConsulUri);
        });
        lastKnownAddress = options.ConsulUri;
    }
}

