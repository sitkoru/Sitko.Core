using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AwesomeAssertions;
using DotNet.Testcontainers.Builders;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.App.Web;
using Sitko.Core.Grpc.Server;
using Sitko.Core.ServiceDiscovery.Consul;
using Xunit;

namespace Sitko.Core.Grpc.Client.Tests
{
    public class ClientAddressSelectionTest
    {
        [Fact]
        public async Task ConfigurationSelectsDiscoveryOrStaticAddressAndDisablesDiscovery()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            await using var consul = new ContainerBuilder("hashicorp/consul:1.21")
                .WithName($"sitko-sd-v04-{Guid.NewGuid():N}")
                .WithCommand("agent", "-dev", "-client=0.0.0.0", "-dns-port=8600")
                .WithPortBinding("8500/tcp", true)
                .WithPortBinding("8600/tcp", true)
                .WithPortBinding("8600/udp", true)
                .WithCreateParameterModifier(static parameters =>
                {
                    ArgumentNullException.ThrowIfNull(parameters.HostConfig);
                    foreach (var bindings in parameters.HostConfig.PortBindings.Values)
                    {
                        foreach (var binding in bindings)
                        {
                            binding.HostIP = "127.0.0.1";
                        }
                    }
                })
                .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(static request =>
                    request.ForPort(8500).ForPath("/v1/status/leader")))
                .Build();
            await consul.StartAsync(cancellationToken);
            using var certificate = CreateCertificate();
            var config = new Dictionary<string, string?>
            {
                ["Application:Environment"] = "sd-v04",
                ["ServiceDiscovery:Consul:Address"] = $"http://127.0.0.1:{consul.GetMappedPublicPort(8500)}",
                ["ServiceDiscovery:Consul:DnsServers:0"] = $"127.0.0.1:{consul.GetMappedPublicPort("8600/udp")}"
            };
            TestContext.Current.TestOutputHelper?.WriteLine(
                $"Own Consul HTTP={config["ServiceDiscovery:Consul:Address"]}; DNS UDP={config["ServiceDiscovery:Consul:DnsServers:0"]}; DNS TCP=127.0.0.1:{consul.GetMappedPublicPort("8600/tcp")}");
            var (discovery, _) = await StartServer<DiscoveryAddressServer>(config, certificate, true, cancellationToken);
            await using var discoveryLifetime = discovery;
            var directConfig = new Dictionary<string, string?>(config) { ["ServiceDiscovery:Enabled"] = "false" };
            var (direct, port) = await StartServer<StaticAddressServer>(directConfig, certificate, true, cancellationToken);
            await using var directLifetime = direct;
            try
            {
                await CallClient(config, certificate, "discovery-ok", cancellationToken);
                config["Grpc:Client:Contracts:accounting.v1.Invoice:Address"] = $"https://127.0.0.1:{port}";
                await CallClient(config, certificate, "static-ok", cancellationToken);
                config["ServiceDiscovery:Enabled"] = "false";
                await consul.StopAsync(cancellationToken);
                await CallClient(config, certificate, "static-ok", cancellationToken);
            }
            finally
            {
                await discovery.StopAsync(cancellationToken);
                await direct.StopAsync(cancellationToken);
            }
        }

        private static async Task CallClient(Dictionary<string, string?> config, X509Certificate2 certificate,
            string expected, CancellationToken cancellationToken)
        {
            var builder = Host.CreateApplicationBuilder();
            _ = builder.Configuration.AddInMemoryCollection(config);
            var app = builder.AddSitkoCore();
            _ = app.AddConsulServiceDiscovery();
            _ = app.AddGrpcClient<Accounting.Invoice.InvoiceClient>(options =>
                DnsTlsContractTest.ConfigureClient(options, certificate));
            using var host = builder.Build();
            await host.StartAsync(cancellationToken);
            try
            {
                var client = host.Services.GetRequiredService<Accounting.Invoice.InvoiceClient>();
                var reply = await client.GetAsync(new Accounting.Request(),
                    deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: cancellationToken);
                _ = reply.Value.Should().Be(expected);
            }
            finally
            {
                await host.StopAsync(cancellationToken);
            }
        }

        internal static async Task<(WebApplication Host, int Port)> StartServer<TService>(
            Dictionary<string, string?> config, X509Certificate2 certificate, bool publish,
            CancellationToken cancellationToken, Action<ISitkoCoreApplicationBuilder>? configure = null) where TService : class
        {
            var builder = WebApplication.CreateBuilder();
            _ = builder.Configuration.AddInMemoryCollection(config);
            _ = builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Application:Web:Ports:gRPC:Port"] = "0",
                ["Application:Web:Ports:gRPC:Protocol"] = "Http2",
                ["Application:Web:Ports:gRPC:UseTLS"] = "true",
                ["Application:Web:Ports:gRPC:ExternalAddress"] = "127.0.0.1"
            });
            _ = builder.WebHost.ConfigureKestrel(options =>
            {
                options.ConfigureEndpointDefaults(listen =>
                {
                    if (listen.IPEndPoint is { } endpoint)
                    {
                        endpoint.Address = IPAddress.Loopback;
                    }
                });
                options.ConfigureHttpsDefaults(https =>
                    https.OnAuthenticate = (_, ssl) =>
                    {
                        ssl.ServerCertificate = certificate;
                        ssl.ServerCertificateContext = SslStreamCertificateContext.Create(certificate, null);
                    });
            });
            var app = builder.AddSitkoCoreWeb();
            if (publish)
            {
                _ = app.AddConsulServiceDiscovery();
            }
            if (configure is null)
            {
                _ = app.AddGrpcServer().RegisterService<TService>();
            }
            else
            {
                configure(app);
            }
            var host = builder.Build();
            _ = host.MapSitkoCore();
            await host.StartAsync(cancellationToken);
            return (host, app.Context.GetModuleInstance<AppWebConfigurationModule>().Listeners["gRPC"].IPEndPoint!.Port);
        }

        internal static X509Certificate2 CreateCertificate()
        {
            using var key = RSA.Create(2048);
            var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var san = new SubjectAlternativeNameBuilder();
            san.AddDnsName("localhost");
            san.AddIpAddress(IPAddress.Loopback);
            request.CertificateExtensions.Add(san.Build());
            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
        }
    }

    public class DiscoveryAddressServer : Accounting.Invoice.InvoiceBase
    {
        public override Task<Accounting.Reply> Get(Accounting.Request request, ServerCallContext context) =>
            Task.FromResult(new Accounting.Reply { Value = "discovery-ok" });
    }

    public class StaticAddressServer : Accounting.Invoice.InvoiceBase
    {
        public override Task<Accounting.Reply> Get(Accounting.Request request, ServerCallContext context) =>
            Task.FromResult(new Accounting.Reply { Value = "static-ok" });
    }
}
