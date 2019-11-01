using System;

namespace Sitko.Core.Grpc.Server
{
    public class GrpcServerOptions
    {
        public string IpAddress { get; set; }
        public string Version { get; set; } = "dev";
        public TimeSpan ChecksInterval { get; set; } = TimeSpan.FromSeconds(60);
        public TimeSpan DeregisterTimeout { get; set; } = TimeSpan.FromSeconds(60);
    }
}
