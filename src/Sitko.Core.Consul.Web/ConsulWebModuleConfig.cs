using System;

namespace Sitko.Core.Consul.Web
{
    public class ConsulWebModuleOptions : ConsulModuleOptions
    {
        public string? IpAddress { get; set; }
        public Uri? ServiceUri { get; set; }

        public TimeSpan ChecksInterval { get; set; } = TimeSpan.FromSeconds(60);
        public TimeSpan DeregisterTimeout { get; set; } = TimeSpan.FromSeconds(60);

        public bool AutoFixRegistration { get; set; } = false;
        public string HealthCheckPath { get; set; } = "/health";
    }
}
