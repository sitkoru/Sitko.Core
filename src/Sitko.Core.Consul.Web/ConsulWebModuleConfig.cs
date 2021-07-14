using System;

namespace Sitko.Core.Consul.Web
{
    public class ConsulWebModuleOptions : ConsulModuleOptions
    {
        public string? IpAddress { get; set; }
        public Uri? ServiceUri { get; set; }

        public int ChecksIntervalInSeconds { get; set; } = 60;
        public int DeregisterTimeoutInSeconds { get; set; } = 60;

        public bool AutoFixRegistration { get; set; }
        public string HealthCheckPath { get; set; } = "/health";
    }
}
