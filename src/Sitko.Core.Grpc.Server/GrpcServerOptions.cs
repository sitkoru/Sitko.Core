using System;

namespace Sitko.Core.Grpc.Server
{
    public class GrpcServerOptions
    {
        public string Version { get; set; } = "dev";
        public string? Host { get; set; }
        public int? Port { get; set; }

        public bool UseTls { get; set; } = true;
        public bool ValidateTls { get; set; } = true;
        public TimeSpan ChecksInterval { get; set; } = TimeSpan.FromSeconds(60);
        public TimeSpan DeregisterTimeout { get; set; } = TimeSpan.FromSeconds(60);
    }
}
