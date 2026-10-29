using System.Net;
using Consul;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.ServiceDiscovery.Consul
{
    public class ConsulServiceDiscoveryModule : BaseApplicationModule<ConsulServiceDiscoveryOptions>
    {
        public override string OptionsKey => "ServiceDiscovery:Consul";
        public override string[] OptionKeys => ["ServiceDiscovery", OptionsKey];

        public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
            ConsulServiceDiscoveryOptions startupOptions)
        {
            startupOptions.Validate();
            _ = services.AddDnsSrvServiceEndpointProvider(options =>
            {
                options.ServiceDomainNameCallback = query => $"{query.ServiceName}.service.consul";
                options.DefaultRefreshPeriod = startupOptions.DnsRefreshPeriod;
                options.MinRetryPeriod = startupOptions.DnsMinRetryPeriod;
                options.MaxRetryPeriod = startupOptions.DnsMaxRetryPeriod;
                options.RetryBackOffFactor = startupOptions.DnsRetryBackOffFactor;
            });
            _ = services.ConfigureDnsResolver(options =>
            {
                options.Servers = [.. startupOptions.DnsServers.Select(IPEndPoint.Parse)];
                options.Timeout = startupOptions.DnsTimeout;
                options.MaxAttempts = startupOptions.DnsMaxAttempts;
            });
            _ = services.AddSingleton<IConsulClient>(_ => new ConsulClient(config =>
            {
                config.Address = startupOptions.Address;
                config.WaitTime = startupOptions.RequestTimeout;
            }, client => client.Timeout = startupOptions.RequestTimeout));
            _ = services.AddHostedService<ConsulRegistrationService>();
        }
    }
}
