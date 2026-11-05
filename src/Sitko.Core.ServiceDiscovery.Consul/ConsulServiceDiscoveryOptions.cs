using System.Net;
using Microsoft.Extensions.ServiceDiscovery.Dns;
using Sitko.Core.App;

namespace Sitko.Core.ServiceDiscovery.Consul
{
    public class ConsulServiceDiscoveryOptions : BaseModuleOptions
    {
        public Uri Address { get; set; } = new("http://localhost:8500");
        public List<string> DnsServers { get; set; } = [];
        public TimeSpan DnsTimeout { get; set; } = new DnsResolverOptions().Timeout;
        public int DnsMaxAttempts { get; set; } = new DnsResolverOptions().MaxAttempts;
        public TimeSpan DnsRefreshPeriod { get; set; } = new DnsSrvServiceEndpointProviderOptions().DefaultRefreshPeriod;
        public TimeSpan DnsMinRetryPeriod { get; set; } = new DnsSrvServiceEndpointProviderOptions().MinRetryPeriod;
        public TimeSpan DnsMaxRetryPeriod { get; set; } = new DnsSrvServiceEndpointProviderOptions().MaxRetryPeriod;
        public double DnsRetryBackOffFactor { get; set; } = new DnsSrvServiceEndpointProviderOptions().RetryBackOffFactor;
        public TimeSpan RegistrationRetryInterval { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan DeregisterCriticalServiceAfter { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan Ttl { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(5);

        public override void Configure(IApplicationContext applicationContext)
        {
            base.Configure(applicationContext);
            if (DnsServers.Count == 0)
            {
                DnsServers = ["127.0.0.1:8600"];
            }
        }

        internal void Validate()
        {
            var invalid = new List<string>();
            foreach (var (field, value) in new (string, TimeSpan)[]
            {
                (nameof(DnsTimeout), DnsTimeout), (nameof(DnsRefreshPeriod), DnsRefreshPeriod),
                (nameof(DnsMinRetryPeriod), DnsMinRetryPeriod), (nameof(DnsMaxRetryPeriod), DnsMaxRetryPeriod),
                (nameof(Ttl), Ttl), (nameof(HeartbeatInterval), HeartbeatInterval),
                (nameof(RequestTimeout), RequestTimeout), (nameof(RegistrationRetryInterval), RegistrationRetryInterval),
                (nameof(DeregisterCriticalServiceAfter), DeregisterCriticalServiceAfter)
            })
            {
                if (value <= TimeSpan.Zero)
                {
                    invalid.Add($"{field} must be positive");
                }
            }
            if (DnsMaxAttempts <= 0)
            {
                invalid.Add($"{nameof(DnsMaxAttempts)} must be positive");
            }
            if (!double.IsFinite(DnsRetryBackOffFactor) || DnsRetryBackOffFactor <= 0)
            {
                invalid.Add($"{nameof(DnsRetryBackOffFactor)} must be positive and finite");
            }
            if (HeartbeatInterval >= Ttl)
            {
                invalid.Add($"{nameof(HeartbeatInterval)} must be less than {nameof(Ttl)}");
            }
            if (DnsMaxRetryPeriod < DnsMinRetryPeriod)
            {
                invalid.Add($"{nameof(DnsMaxRetryPeriod)} must be at least {nameof(DnsMinRetryPeriod)}");
            }
            if (!Address.IsAbsoluteUri || (Address.Scheme != Uri.UriSchemeHttp && Address.Scheme != Uri.UriSchemeHttps))
            {
                invalid.Add($"{nameof(Address)} must be an absolute HTTP(S) URI");
            }
            if (DnsServers.Count == 0 || DnsServers.Any(static server =>
                !IPEndPoint.TryParse(server, out var endpoint) || endpoint.Port <= 0))
            {
                invalid.Add($"{nameof(DnsServers)} must contain IP endpoints with positive ports");
            }
            if (invalid.Count > 0)
            {
                throw new InvalidOperationException($"ServiceDiscovery:Consul: {string.Join("; ", invalid)}");
            }
        }
    }
}
