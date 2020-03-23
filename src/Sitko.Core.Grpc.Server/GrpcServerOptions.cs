using System;

namespace Sitko.Core.Grpc.Server
{
    public class GrpcServerOptions
    {
        public string Version { get; set; } = "dev";
        public string? Host { get; set; }
        public TimeSpan ChecksInterval { get; set; } = TimeSpan.FromSeconds(60);
        public TimeSpan DeregisterTimeout { get; set; } = TimeSpan.FromSeconds(60);
    }
}
