using System;
using System.Collections.Generic;
using Grpc.AspNetCore.Server;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Server
{
    public class GrpcServerOptions : BaseModuleConfig
    {
        public string? Host { get; set; }
        public int? Port { get; set; }

        public TimeSpan ChecksInterval { get; set; } = TimeSpan.FromSeconds(60);
        public TimeSpan DeregisterTimeout { get; set; } = TimeSpan.FromSeconds(60);

        public bool EnableReflection { get; set; } = false;
        public bool EnableDetailedErrors { get; set; } = false;
        public Action<GrpcServiceOptions>? Configure { get; set; }

        public bool AutoFixRegistration { get; set; } = false;

        public Action<IGrpcServerModule>[] ServiceRegistrations => _serviceRegistrations.ToArray();

        private readonly List<Action<IGrpcServerModule>> _serviceRegistrations = new();

        public GrpcServerOptions RegisterService<TService>() where TService : class
        {
            _serviceRegistrations.Add(module => module.RegisterService<TService>());
            return this;
        }
    }
}
