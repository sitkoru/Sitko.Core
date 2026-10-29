using System.Net;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Sitko.Core.App.Web;
using Sitko.Core.Grpc.Server;
using Sitko.Core.ServiceDiscovery.Consul;
using Xunit;

namespace Sitko.Core.Grpc.Client.Tests
{
    public class DiscoveryListenerStartupTest
    {
        [Theory]
        [InlineData("0.0.0.0")]
        [InlineData("::")]
        public async Task WildcardPublicationAddressFailsApplicationStartup(string address)
        {
            var token = TestContext.Current.CancellationToken;
            await using var consul = DiscoveryLifecycleTest.CreateConsul();
            await consul.StartAsync(token);
            var config = DiscoveryLifecycleTest.Configuration(consul);
            config["Application:Web:Ports:gRPC:Port"] = "0";
            config["Application:Web:Ports:gRPC:Protocol"] = "Http2";
            config["Application:Web:Ports:gRPC:UseTLS"] = "true";
            config["Application:Web:Ports:gRPC:ExternalAddress"] = address;
            var builder = WebApplication.CreateBuilder();
            _ = builder.Configuration.AddInMemoryCollection(config);
            _ = builder.WebHost.ConfigureKestrel(options => options.ConfigureEndpointDefaults(listen =>
            {
                if (listen.IPEndPoint is { } endpoint)
                {
                    endpoint.Address = IPAddress.Loopback;
                }
            }));
            var app = builder.AddSitkoCoreWeb();
            _ = app.AddConsulServiceDiscovery();
            _ = app.AddGrpcServer().RegisterService<GenerationOneServer>();
            await using var host = builder.Build();
            _ = host.MapSitkoCore();
            try
            {
                var error = await Assert.ThrowsAnyAsync<Exception>(() => host.StartAsync(token));
                _ = error.ToString().Should().Contain("accounting.v1.Invoice").And.Contain("ExternalAddress");
            }
            finally
            {
                await host.StopAsync(token);
            }
        }
    }
}
