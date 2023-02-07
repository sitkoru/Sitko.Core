using System.Text.Json.Serialization;
using Grpc.AspNetCore.Server;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Server;

[PublicAPI]
public class GrpcServerModuleOptions : BaseModuleOptions
{
    private readonly List<Action<IGrpcServerModule>> serviceRegistrations = new();
    internal string? RequiredAuthorizarionSchemeName { get; private set; }
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
        serviceRegistrations.Add(module => module.RegisterService<TService>(RequiredAuthorizarionSchemeName));
        return this;
    }

    public GrpcServerModuleOptions RequireSchemeAuthorization(string schemeName)
    {
        RequiredAuthorizarionSchemeName = schemeName;
        return this;
    }
}
