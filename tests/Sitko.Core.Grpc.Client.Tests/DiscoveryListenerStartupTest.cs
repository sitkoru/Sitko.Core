using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App.Web;
using Sitko.Core.Grpc.Server;
using Sitko.Core.ServiceDiscovery.Consul;
using Xunit;

namespace Sitko.Core.Grpc.Client.Tests
{
    public class DiscoveryListenerStartupTest
    {
        [Theory]
        [InlineData("127.0.0.1", "127.0.0.1")]
        [InlineData("0.0.0.0", "127.0.0.1")]
        [InlineData("::1", "::1")]
        [InlineData("::", "::1")]
        public async Task RegistersActualHttpsAddressWhenPortsAreNotConfigured(string listenAddress, string expectedAddress)
        {
            var token = TestContext.Current.CancellationToken;
            await using var consul = DiscoveryLifecycleTest.CreateConsul();
            await consul.StartAsync(token);
            using var certificate = ClientAddressSelectionTest.CreateCertificate();
            var config = DiscoveryLifecycleTest.Configuration(consul);
            var builder = WebApplication.CreateBuilder();
            _ = builder.Configuration.AddInMemoryCollection(config);
            _ = builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Parse(listenAddress), 0, listen =>
            {
                listen.Protocols = HttpProtocols.Http2;
                _ = listen.UseHttps(certificate);
            }));
            var app = builder.AddSitkoCoreWeb();
            _ = app.AddConsulServiceDiscovery();
            _ = app.AddGrpcServer().RegisterService<GenerationOneServer>();
            await using var host = builder.Build();
            _ = host.MapSitkoCore();
            await host.StartAsync(token);
            try
            {
                var addresses = host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
                var actual = new Uri(addresses.Addresses.Single());
                _ = actual.Port.Should().BeGreaterThan(0);
                using var agent = new HttpClient { BaseAddress = new Uri(config["ServiceDiscovery:Consul:Address"]!) };
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeout.CancelAfter(TimeSpan.FromSeconds(15));
                while (true)
                {
                    await using var stream = await agent.GetStreamAsync("/v1/agent/services", timeout.Token);
                    using var json = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token);
                    var registrations = json.RootElement.EnumerateObject().ToArray();
                    if (registrations.Length > 0)
                    {
                        var registration = Assert.Single(registrations).Value;
                        _ = registration.GetProperty("Address").GetString().Should().Be(expectedAddress);
                        _ = registration.GetProperty("Port").GetInt32().Should().Be(actual.Port);
                        using var connection = new TcpClient();
                        await connection.ConnectAsync(registration.GetProperty("Address").GetString()!, actual.Port, timeout.Token);
                        break;
                    }
                    await Task.Delay(100, timeout.Token);
                }
            }
            finally
            {
                await host.StopAsync(token);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task MissingOrAmbiguousHttpsAddressFailsApplicationStartup(bool multipleHttps)
        {
            var token = TestContext.Current.CancellationToken;
            await using var consul = DiscoveryLifecycleTest.CreateConsul();
            await consul.StartAsync(token);
            using var certificate = ClientAddressSelectionTest.CreateCertificate();
            var builder = WebApplication.CreateBuilder();
            _ = builder.Configuration.AddInMemoryCollection(DiscoveryLifecycleTest.Configuration(consul));
            _ = builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Loopback, 0, listen =>
                {
                    if (multipleHttps)
                    {
                        _ = listen.UseHttps(certificate);
                    }
                });
                if (multipleHttps)
                {
                    options.Listen(IPAddress.Loopback, 0, listen => listen.UseHttps(certificate));
                }
            });
            var app = builder.AddSitkoCoreWeb();
            _ = app.AddConsulServiceDiscovery();
            _ = app.AddGrpcServer().RegisterService<GenerationOneServer>();
            await using var host = builder.Build();
            _ = host.MapSitkoCore();
            try
            {
                var error = await Assert.ThrowsAnyAsync<Exception>(() => host.StartAsync(token));
                _ = error.ToString().Should().Contain("accounting.v1.Invoice").And.Contain("one bound HTTPS address");
            }
            finally
            {
                await host.StopAsync(token);
            }
        }

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
