using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AwesomeAssertions;
using DotNet.Testcontainers.Builders;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
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
    public class DnsTlsContractTest
    {
        [Fact]
        public async Task SameShortNameContractsUseDnsTlsAndPerCallCredentials()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            await using var consul = new ContainerBuilder()
                .WithImage("hashicorp/consul:1.21")
                .WithName($"sitko-sd-v01-{Guid.NewGuid():N}")
                .WithCommand("agent", "-dev", "-client=0.0.0.0", "-dns-port=8600")
                .WithPortBinding("8500/tcp", true)
                .WithPortBinding("8600/tcp", true)
                .WithPortBinding("8600/udp", true)
                .WithCreateParameterModifier(parameters =>
                {
                    foreach (var bindings in parameters.HostConfig.PortBindings.Values)
                    {
                        foreach (var binding in bindings)
                        {
                            binding.HostIP = "127.0.0.1";
                        }
                    }
                })
                .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request =>
                    request.ForPort(8500).ForPath("/v1/status/leader")))
                .Build();
            await consul.StartAsync(cancellationToken);
            using var certificate = CreateCertificate();
            var config = new Dictionary<string, string?>
            {
                ["Application:Environment"] = "sd-v01",
                ["ServiceDiscovery:Consul:DnsRefreshPeriod"] = "00:00:01",
                // Первый пустой DNS-ответ ожидает MaxRetryPeriod, а не DnsRefreshPeriod.
                ["ServiceDiscovery:Consul:DnsMinRetryPeriod"] = "00:00:00.100",
                ["ServiceDiscovery:Consul:DnsMaxRetryPeriod"] = "00:00:01",
                ["ServiceDiscovery:Consul:Address"] = $"http://127.0.0.1:{consul.GetMappedPublicPort(8500)}",
                ["ServiceDiscovery:Consul:DnsServers:0"] = $"127.0.0.1:{consul.GetMappedPublicPort("8600/udp")}"
            };
            TestContext.Current.TestOutputHelper?.WriteLine(
                $"Own Consul HTTP={config["ServiceDiscovery:Consul:Address"]}; DNS UDP={config["ServiceDiscovery:Consul:DnsServers:0"]}; DNS TCP=127.0.0.1:{consul.GetMappedPublicPort("8600/tcp")}");
            var serverBuilder = WebApplication.CreateBuilder();
            _ = serverBuilder.Configuration.AddInMemoryCollection(config);
            _ = serverBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Application:Web:Ports:gRPC:Port"] = "0",
                ["Application:Web:Ports:gRPC:Protocol"] = "Http2",
                ["Application:Web:Ports:gRPC:UseTLS"] = "true",
                ["Application:Web:Ports:gRPC:ExternalAddress"] = "127.0.0.1"
            });
            _ = serverBuilder.WebHost.ConfigureKestrel(options =>
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
            var serverApp = serverBuilder.AddSitkoCoreWeb();
            _ = serverApp.AddConsulServiceDiscovery();
            _ = serverApp.AddGrpcServer().RegisterService<AccountingServer>().RegisterService<ReportsServer>();
            await using var server = serverBuilder.Build();
            _ = server.MapSitkoCore();
            await server.StartAsync(cancellationToken);
            var endpoint = serverApp.Context.GetModuleInstance<AppWebConfigurationModule>().Listeners["gRPC"].IPEndPoint!;
            _ = endpoint.Address.Should().Be(IPAddress.Loopback);
            _ = endpoint.Port.Should().BeGreaterThan(0);
            try
            {
                var clientBuilder = Host.CreateApplicationBuilder();
                _ = clientBuilder.Configuration.AddInMemoryCollection(config);
                var clientApp = clientBuilder.AddSitkoCore();
                _ = clientApp.AddConsulServiceDiscovery();
                _ = clientApp.AddGrpcClient<Accounting.Invoice.InvoiceClient>(options => ConfigureClient(options, certificate));
                _ = clientApp.AddReportsClientModule(certificate);
                using var client = clientBuilder.Build();
                await client.StartAsync(cancellationToken);
                try
                {
                    _ = client.Services.GetService<IServer>().Should().BeNull();
                    var accounting = client.Services.GetRequiredService<Accounting.Invoice.InvoiceClient>();
                    var reports = client.Services.GetRequiredService<Reports.Invoice.InvoiceClient>();
                    var accountingReply = await WaitForEndpoint("accounting.v1.Invoice", deadline =>
                        accounting.GetAsync(new Accounting.Request(), deadline: deadline,
                            cancellationToken: cancellationToken).ResponseAsync, cancellationToken);
                    var reportsReply = await WaitForEndpoint("reports.v1.Invoice", deadline =>
                        reports.GetAsync(new Reports.Request(), deadline: deadline,
                            cancellationToken: cancellationToken).ResponseAsync, cancellationToken);
                    _ = accountingReply.Value.Should().Be("accounting-ok");
                    _ = reportsReply.Value.Should().Be("reports-ok");
                }
                finally
                {
                    await client.StopAsync(cancellationToken);
                }
            }
            finally
            {
                await server.StopAsync(cancellationToken);
            }
        }

        private static async Task<TReply> WaitForEndpoint<TReply>(string contract,
            Func<DateTime, Task<TReply>> call, CancellationToken cancellationToken)
        {
            var elapsed = Stopwatch.StartNew();
            var timeout = TimeSpan.FromSeconds(30);
            RpcException? lastError = null;
            while (elapsed.Elapsed < timeout)
            {
                try
                {
                    var reply = await call(DateTime.UtcNow.Add(timeout - elapsed.Elapsed));
                    TestContext.Current.TestOutputHelper?.WriteLine($"{contract} ready after {elapsed.Elapsed}");
                    return reply;
                }
                catch (RpcException exception) when (exception.StatusCode == StatusCode.Internal &&
                    exception.Status.DebugException is InvalidOperationException
                    { Message: "The endpoint collection contains no endpoints" })
                {
                    // Each contract is published asynchronously after the host starts; TLS and server failures still fail immediately.
                    if (lastError is null)
                    {
                        TestContext.Current.TestOutputHelper?.WriteLine($"{contract}: waiting for its DNS endpoint");
                    }
                    lastError = exception;
                }
                await Task.Delay(100, cancellationToken);
            }
            throw new TimeoutException($"{contract} had no discovered endpoint within {timeout}", lastError);
        }

        internal static void ConfigureClient<TClient>(GrpcClientModuleOptions<TClient> options, X509Certificate2 certificate)
            where TClient : ClientBase<TClient>
        {
            options.DefaultDeadline = null;
            _ = options.AddTokenAuth<TestTokenProvider>();
            options.ConfigureHttpHandler = handler =>
            {
                var sockets = (SocketsHttpHandler)handler;
                sockets.SslOptions.RemoteCertificateValidationCallback = (_, remote, _, _) =>
                    remote is not null && remote.GetCertHashString(HashAlgorithmName.SHA256) ==
                    certificate.GetCertHashString(HashAlgorithmName.SHA256);
                return handler;
            };
        }

        private static X509Certificate2 CreateCertificate()
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

    public class TestTokenProvider : IGrpcTokenProvider
    {
        public Task<string> GetTokenAsync() => Task.FromResult("Bearer v01-token");
    }

    public class AccountingServer : Accounting.Invoice.InvoiceBase
    {
        public override Task<Accounting.Reply> Get(Accounting.Request request, ServerCallContext context)
        {
            _ = context.RequestHeaders.GetValue("authorization").Should().Be("Bearer v01-token");
            return Task.FromResult(new Accounting.Reply { Value = "accounting-ok" });
        }
    }

    public class ReportsServer : Reports.Invoice.InvoiceBase
    {
        public override Task<Reports.Reply> Get(Reports.Request request, ServerCallContext context)
        {
            _ = context.RequestHeaders.GetValue("authorization").Should().Be("Bearer v01-token");
            return Task.FromResult(new Reports.Reply { Value = "reports-ok" });
        }
    }

    public static class ReportsClientRegistration
    {
        public static ISitkoCoreApplicationBuilder AddReportsClientModule(this ISitkoCoreApplicationBuilder builder) =>
            builder.AddModule<ReportsClientModule>().AddGrpcClient<Reports.Invoice.InvoiceClient>();

        public static ISitkoCoreApplicationBuilder AddReportsClientModule(this ISitkoCoreApplicationBuilder builder,
            X509Certificate2 certificate) =>
            builder.AddModule<ReportsClientModule>()
                .AddGrpcClient<Reports.Invoice.InvoiceClient>(options =>
                    DnsTlsContractTest.ConfigureClient(options, certificate));
    }

    public class ReportsClientModule : BaseApplicationModule
    {
        public override string OptionsKey => "ReportsClient";
        public override IEnumerable<Type> GetRequiredModules(IApplicationContext applicationContext,
            BaseApplicationModuleOptions options) => [typeof(IGrpcClientModule<Reports.Invoice.InvoiceClient>)];
    }

}
