using System.Text.Json.Serialization;
using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Server;

public class GrpcServerModuleOptions : BaseModuleOptions
{
    internal string? RequiredAuthorizationSchemeName { get; private set; }
    public string? Host { get; set; }
    public int? Port { get; set; }
    public List<string> ServiceDiscoveryPortNames { get; set; } = ["gRPC", "https"];
    [JsonIgnore] public Action<IWebHostBuilder>? ConfigureWebHostDefaults { get; set; }
    public bool EnableReflection { get; set; }
    public bool? EnableDetailedErrors { get; set; }
    [JsonIgnore] public Action<GrpcServiceOptions>? ConfigureGrpcService { get; set; }

    public GrpcServerModuleOptions RequireSchemeAuthorization(string schemeName)
    {
        RequiredAuthorizationSchemeName = schemeName;
        return this;
    }

}
