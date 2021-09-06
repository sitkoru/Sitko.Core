using Microsoft.AspNetCore.Hosting;

namespace Sitko.Core.Grpc.Server
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using App;
    using global::Grpc.AspNetCore.Server;
    using JetBrains.Annotations;

    [PublicAPI]
    public class GrpcServerModuleOptions : BaseModuleOptions
    {
        private readonly List<Action<IGrpcServerModule>> serviceRegistrations = new();
        public string? Host { get; set; }
        public int? Port { get; set; }
        [JsonIgnore] public Action<IWebHostBuilder>? ConfigureWebHostDefaults { get; set; }

        public int ChecksIntervalInSeconds { get; set; } = 60;
        public int DeregisterTimeoutInSeconds { get; set; } = 60;

        public bool EnableReflection { get; set; }
        public bool EnableDetailedErrors { get; set; }

        [JsonIgnore] public Action<GrpcServiceOptions>? ConfigureGrpcService { get; set; }

        public bool AutoFixRegistration { get; set; }

        [JsonIgnore] public Action<IGrpcServerModule>[] ServiceRegistrations => serviceRegistrations.ToArray();

        public GrpcServerModuleOptions RegisterService<TService>() where TService : class
        {
            serviceRegistrations.Add(module => module.RegisterService<TService>());
            return this;
        }
    }
}
