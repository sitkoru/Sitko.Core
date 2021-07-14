using System;
using System.Collections.Generic;
using Grpc.AspNetCore.Server;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Server
{
    using System.Text.Json.Serialization;

    public class GrpcServerModuleOptions : BaseModuleOptions
    {
        private readonly List<Action<IGrpcServerModule>> _serviceRegistrations = new();
        public string? Host { get; set; }
        public int? Port { get; set; }

        public int ChecksIntervalInSeconds { get; set; } = 60;
        public int DeregisterTimeoutInSeconds { get; set; } = 60;

        public bool EnableReflection { get; set; } = false;
        public bool EnableDetailedErrors { get; set; } = false;

        [JsonIgnore] public Action<GrpcServiceOptions>? Configure { get; set; }

        public bool AutoFixRegistration { get; set; } = false;

        [JsonIgnore] public Action<IGrpcServerModule>[] ServiceRegistrations => _serviceRegistrations.ToArray();

        public GrpcServerModuleOptions RegisterService<TService>() where TService : class
        {
            _serviceRegistrations.Add(module => module.RegisterService<TService>());
            return this;
        }
    }
}
