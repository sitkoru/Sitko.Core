using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Sitko.Core.App.Web;
using Sitko.Core.Grpc.Server;
using Sitko.Core.Xunit.Web;

namespace Sitko.Core.Grpc.Client.Tests;

public abstract class GrpcClientScope : WebTestScope
{
    protected static readonly ConcurrentStack<int> Ports =
        new(Enumerable.Range(8080, 20).OrderBy(x => Guid.NewGuid()));

    protected readonly int GrpcPort;

    protected readonly int HttpPort;

    protected GrpcClientScope()
    {
        if (Ports.TryPop(out var httpPort))
        {
            HttpPort = httpPort;
        }

        if (Ports.TryPop(out var grpcPort))
        {
            GrpcPort = grpcPort;
        }
    }

    protected override bool UseTestServer => false;

    protected override WebApplicationBuilder ConfigureWebApplication(WebApplicationBuilder webApplicationBuilder,
        string name)
    {
        var portsConfig = $$"""
                            {
                              "Application": {
                                "Web": {
                                  "Ports": {
                                    "Http": {
                                      "Port": {{HttpPort}},
                                      "UseTLS": true
                                    },
                                    "gRPC": {
                                      "Port": {{GrpcPort}},
                                      "Protocol": "Http2",
                                      "UseTLS": true
                                    }
                                  }
                                }
                              }
                            }
                            """;
        var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(portsConfig));
        webApplicationBuilder.Configuration.AddJsonStream(jsonStream);
        base.ConfigureWebApplication(webApplicationBuilder, name);
        webApplicationBuilder.AddSitkoCoreWeb()
            .AddGrpcServer(RegisterService);

        return webApplicationBuilder;
    }

    protected virtual void RegisterService(GrpcServerModuleOptions options) =>
        options.RegisterService<TestServiceServer>();
}
