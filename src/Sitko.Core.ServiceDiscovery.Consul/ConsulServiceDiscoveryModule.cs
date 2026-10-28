using System.Net;
using Consul;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.ServiceDiscovery.Consul
{
    public class ConsulServiceDiscoveryModule : BaseApplicationModule<ConsulServiceDiscoveryOptions>, IServiceDiscoveryModule
    {
        public override string OptionsKey => "ServiceDiscovery:Consul";
        public override string[] OptionKeys => ["ServiceDiscovery", OptionsKey];

        public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
            ConsulServiceDiscoveryOptions startupOptions)
        {
            _ = services.AddServiceDiscoveryCore();
            _ = services.AddDnsSrvServiceEndpointProvider(options =>
            {
                options.ServiceDomainNameCallback = query => $"{query.ServiceName}.service.consul";
                options.DefaultRefreshPeriod = TimeSpan.FromSeconds(5);
                options.MinRetryPeriod = TimeSpan.FromSeconds(1);
            });
            _ = services.ConfigureDnsResolver(options =>
                options.Servers = [.. startupOptions.DnsServers.Select(IPEndPoint.Parse)]);
            _ = services.AddSingleton<IConsulClient>(_ => new ConsulClient(config =>
            {
                config.Address = startupOptions.Address;
                config.WaitTime = startupOptions.RequestTimeout;
            }, client => client.Timeout = startupOptions.RequestTimeout));
            _ = services.AddHostedService<ConsulRegistrationService>();
        }
    }

    public class ConsulServiceDiscoveryOptions : BaseModuleOptions
    {
        public Uri Address { get; set; } = new("http://localhost:8500");
        public List<string> DnsServers { get; set; } = ["127.0.0.1:8600"];
        public TimeSpan Ttl { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(5);
    }

    public static class ApplicationExtensions
    {
        public static ISitkoCoreApplicationBuilder AddConsulServiceDiscovery(this ISitkoCoreApplicationBuilder builder,
            Action<ConsulServiceDiscoveryOptions>? configure = null) =>
            builder.AddModule<ConsulServiceDiscoveryModule, ConsulServiceDiscoveryOptions>(configure);
    }
}
