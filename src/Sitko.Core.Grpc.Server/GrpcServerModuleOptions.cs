using System.Text.Json.Serialization;
using Grpc.AspNetCore.Server;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Server;

[PublicAPI]
public class GrpcServerModuleOptions : BaseModuleOptions
{
    private readonly Dictionary<string, Action<IGrpcServerModule>> serviceRegistrations = new();
    internal string? RequiredAuthorizationSchemeName { get; private set; }
    internal bool EnableGrpcWeb { get; private set; }
    public string? Host { get; set; }
    public int? Port { get; set; }
    public bool EnableServiceDiscovery { get; set; } = true;
    public List<string> ServiceDiscoveryPortNames { get; set; } = ["gRPC", "https"];
    [JsonIgnore] public Action<IWebHostBuilder>? ConfigureWebHostDefaults { get; set; }

    public bool EnableReflection { get; set; }
    public bool EnableDetailedErrors { get; set; }

    [JsonIgnore] public Action<GrpcServiceOptions>? ConfigureGrpcService { get; set; }

    [JsonIgnore]
    internal IReadOnlyDictionary<string, Action<IGrpcServerModule>> ServiceRegistrations =>
        serviceRegistrations.AsReadOnly();

    public GrpcServerModuleOptions RegisterService<TService>() where TService : class
    {
        serviceRegistrations.Add(GrpcServicesHelper.GetServiceName<TService>(),
            module => module.RegisterService<TService>(RequiredAuthorizationSchemeName));
        return this;
    }

    public GrpcServerModuleOptions RegisterServiceWithGrpcWeb<TService>() where TService : class
    {
        EnableGrpcWeb = true;
        serviceRegistrations.Add(GrpcServicesHelper.GetServiceName<TService>(),
            module => module.RegisterService<TService>(RequiredAuthorizationSchemeName, true));
        return this;
    }

    public GrpcServerModuleOptions RequireSchemeAuthorization(string schemeName)
    {
        RequiredAuthorizationSchemeName = schemeName;
        return this;
    }
}
