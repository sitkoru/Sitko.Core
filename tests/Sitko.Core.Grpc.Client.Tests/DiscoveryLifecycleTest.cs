using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using AwesomeAssertions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.ServiceDiscovery.Consul;
using Xunit;

namespace Sitko.Core.Grpc.Client.Tests
{
    public class DiscoveryLifecycleTest
    {
        [Fact]
        public async Task SameClientReachesNewGenerationAfterGracefulStopAndPortChange()
        {
            var token = TestContext.Current.CancellationToken;
            await using var consul = CreateConsul();
            await consul.StartAsync(token);
            using var certificate = ClientAddressSelectionTest.CreateCertificate();
            var config = Configuration(consul);
            config["Application:Web:Ports:http:Port"] = "0";
            config["Application:Web:Ports:http:Protocol"] = "Http1";
            config["Application:Web:Ports:http:UseTLS"] = "false";
            var (first, firstPort) = await ClientAddressSelectionTest.StartServer<GenerationOneServer>(
                config, certificate, true, token);
            await using var firstLifetime = first;
            using var clientHost = CreateClient(config, certificate);
            await clientHost.StartAsync(token);
            try
            {
                var client = clientHost.Services.GetRequiredService<Accounting.Invoice.InvoiceClient>();
                await WaitForReply(client, "generation-1", token);
                await first.StopAsync(token);
                var absent = Stopwatch.StartNew();
                _ = await Assert.ThrowsAsync<RpcException>(async () =>
                    await client.GetAsync(new Accounting.Request(), deadline: DateTime.UtcNow.AddSeconds(2), cancellationToken: token));
                _ = absent.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));

                // Keep the old port occupied so the OS cannot reuse it for the new generation.
                using var oldPort = new TcpListener(IPAddress.Loopback, firstPort);
                oldPort.Start();
                var (second, secondPort) = await ClientAddressSelectionTest.StartServer<GenerationTwoServer>(
                    config, certificate, true, token);
                await using var secondLifetime = second;
                try
                {
                    _ = secondPort.Should().NotBe(firstPort);
                    _ = secondPort.Should().BeGreaterThan(0);
                    await WaitForReply(client, "generation-2", token);
                }
                finally
                {
                    await second.StopAsync(token);
                }
            }
            finally
            {
                await first.StopAsync(token);
                await clientHost.StopAsync(token);
            }
        }

        [Fact]
        public async Task SameClientExcludesKilledReplicaAfterTtl()
        {
            var token = TestContext.Current.CancellationToken;
            await using var consul = CreateConsul();
            await consul.StartAsync(token);
            using var certificate = ClientAddressSelectionTest.CreateCertificate();
            var certificatePath = Path.Combine(Path.GetTempPath(), $"sitko-sd-{Guid.NewGuid():N}.pfx");
            await File.WriteAllBytesAsync(certificatePath, certificate.Export(X509ContentType.Pfx), token);
            try
            {
                var config = Configuration(consul);
                await using var first = await DiscoveryChildProcess.Start(config, certificatePath, "replica-a", token);
                await using var second = await DiscoveryChildProcess.Start(config, certificatePath, "replica-b", token);
                _ = first.Port.Should().BeGreaterThan(0);
                _ = second.Port.Should().BeGreaterThan(0).And.NotBe(first.Port);
                using var clientHost = CreateClient(config, certificate);
                await clientHost.StartAsync(token);
                try
                {
                    var client = clientHost.Services.GetRequiredService<Accounting.Invoice.InvoiceClient>();
                    await WaitForReply(client, "replica-a", token);
                    await WaitForReply(client, "replica-b", token);
                    using var agent = new HttpClient
                    {
                        BaseAddress = new Uri(config["ServiceDiscovery:Consul:Address"]!),
                        Timeout = TimeSpan.FromSeconds(2)
                    };
                    var before = await ReadRegistrations(agent, token);
                    var firstId = before.Single(entry => entry.Value == first.Port).Key;
                    var secondId = before.Single(entry => entry.Value == second.Port).Key;
                    var transition = Stopwatch.StartNew();
                    await first.Kill(token);
                    var afterKill = await ReadRegistrations(agent, token);
                    _ = afterKill.Should().ContainKey(firstId).And.ContainKey(secondId);
                    await WaitForCritical(agent, firstId, transition, token);
                    await WaitForTwentyReplies(client, transition, token);
                    await second.Stop(token);
                }
                finally
                {
                    await clientHost.StopAsync(token);
                }
            }
            finally
            {
                File.Delete(certificatePath);
            }
        }

        [Fact]
        public async Task GracefulStopRemovesOnlyOwnReplica()
        {
            var token = TestContext.Current.CancellationToken;
            await using var consul = CreateConsul();
            await consul.StartAsync(token);
            using var certificate = ClientAddressSelectionTest.CreateCertificate();
            var certificatePath = Path.Combine(Path.GetTempPath(), $"sitko-sd-{Guid.NewGuid():N}.pfx");
            await File.WriteAllBytesAsync(certificatePath, certificate.Export(X509ContentType.Pfx), token);
            try
            {
                var config = Configuration(consul);
                await using var first = await DiscoveryChildProcess.Start(config, certificatePath, "replica-a", token);
                await using var second = await DiscoveryChildProcess.Start(config, certificatePath, "replica-b", token);
                _ = first.Port.Should().BeGreaterThan(0);
                _ = second.Port.Should().BeGreaterThan(0).And.NotBe(first.Port);
                using var clientHost = CreateClient(config, certificate);
                await clientHost.StartAsync(token);
                try
                {
                    var client = clientHost.Services.GetRequiredService<Accounting.Invoice.InvoiceClient>();
                    await WaitForReply(client, "replica-a", token);
                    await WaitForReply(client, "replica-b", token);
                    using var agent = new HttpClient
                    {
                        BaseAddress = new Uri(config["ServiceDiscovery:Consul:Address"]!),
                        Timeout = TimeSpan.FromSeconds(2)
                    };
                    var before = await ReadRegistrations(agent, token);
                    var firstId = before.Single(entry => entry.Value == first.Port).Key;
                    var secondId = before.Single(entry => entry.Value == second.Port).Key;
                    var transition = Stopwatch.StartNew();
                    await first.Stop(token);
                    var afterStop = await ReadRegistrations(agent, token);
                    _ = afterStop.Should().NotContainKey(firstId).And.ContainKey(secondId);
                    await WaitForTwentyReplies(client, transition, token);
                    await second.Stop(token);
                }
                finally
                {
                    await clientHost.StopAsync(token);
                }
            }
            finally
            {
                File.Delete(certificatePath);
            }
        }

        private static async Task<Dictionary<string, int>> ReadRegistrations(HttpClient agent,
            CancellationToken token)
        {
            await using var stream = await agent.GetStreamAsync("/v1/agent/services", token);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: token);
            return json.RootElement.EnumerateObject().ToDictionary(
                static service => service.Name, static service => service.Value.GetProperty("Port").GetInt32());
        }

        private static async Task WaitForCritical(HttpClient agent, string serviceId,
            Stopwatch transition, CancellationToken token)
        {
            while (true)
            {
                _ = transition.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(58));
                await using var stream = await agent.GetStreamAsync("/v1/agent/checks", token);
                using var json = await JsonDocument.ParseAsync(stream, cancellationToken: token);
                if (json.RootElement.EnumerateObject().Any(check =>
                        check.Value.GetProperty("ServiceID").GetString() == serviceId &&
                        check.Value.GetProperty("Status").GetString() == "critical"))
                {
                    TestContext.Current.TestOutputHelper?.WriteLine($"Killed replica became critical after {transition.Elapsed}");
                    return;
                }
                await Task.Delay(100, token);
            }
        }

        private static async Task WaitForTwentyReplies(Accounting.Invoice.InvoiceClient client,
            Stopwatch transition, CancellationToken token)
        {
            var consecutive = 0;
            while (consecutive < 20)
            {
                _ = transition.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(58));
                try
                {
                    var reply = await client.GetAsync(new Accounting.Request(),
                        deadline: DateTime.UtcNow.AddSeconds(2), cancellationToken: token);
                    _ = reply.Value.Should().Be("replica-b");
                    consecutive++;
                }
                catch (RpcException) when (!token.IsCancellationRequested)
                {
                    consecutive = 0;
                    await Task.Delay(100, token);
                }
            }
            _ = transition.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(60));
            TestContext.Current.TestOutputHelper?.WriteLine($"20 consecutive replica-b RPCs after {transition.Elapsed}");
        }

        internal static IContainer CreateConsul() => new ContainerBuilder()
            .WithImage("hashicorp/consul:1.21")
            .WithName($"sitko-sd-lifecycle-{Guid.NewGuid():N}")
            .WithCommand("agent", "-dev", "-client=0.0.0.0", "-dns-port=8600")
            .WithPortBinding("8500/tcp", true)
            .WithPortBinding("8600/tcp", true)
            .WithPortBinding("8600/udp", true)
            .WithCreateParameterModifier(static parameters =>
            {
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

        internal static Dictionary<string, string?> Configuration(IContainer consul) => new()
        {
            ["Application:Environment"] = "sd-lifecycle",
            ["ServiceDiscovery:Consul:Address"] = $"http://127.0.0.1:{consul.GetMappedPublicPort(8500)}",
            ["ServiceDiscovery:Consul:DnsServers:0"] = $"127.0.0.1:{consul.GetMappedPublicPort("8600/udp")}",
            ["ServiceDiscovery:Consul:DnsTimeout"] = "00:00:01",
            ["ServiceDiscovery:Consul:DnsMaxAttempts"] = "1",
            ["ServiceDiscovery:Consul:DnsRefreshPeriod"] = "00:00:01",
            ["ServiceDiscovery:Consul:DnsMinRetryPeriod"] = "00:00:00.100",
            ["ServiceDiscovery:Consul:DnsMaxRetryPeriod"] = "00:00:01",
            ["ServiceDiscovery:Consul:Ttl"] = "00:00:05",
            ["ServiceDiscovery:Consul:HeartbeatInterval"] = "00:00:01",
            ["ServiceDiscovery:Consul:RegistrationRetryInterval"] = "00:00:00.200",
            ["ServiceDiscovery:Consul:RequestTimeout"] = "00:00:01"
        };

        internal static IHost CreateClient(Dictionary<string, string?> config, X509Certificate2 certificate)
        {
            var builder = Host.CreateApplicationBuilder();
            _ = builder.Configuration.AddInMemoryCollection(config);
            var app = builder.AddSitkoCore();
            _ = app.AddConsulServiceDiscovery();
            _ = app.AddGrpcClient<Accounting.Invoice.InvoiceClient>(options =>
            {
                DnsTlsContractTest.ConfigureClient(options, certificate);
                options.RetryPolicy = null;
            });
            return builder.Build();
        }

        internal static async Task WaitForReply(Accounting.Invoice.InvoiceClient client, string expected,
            CancellationToken token)
        {
            var elapsed = Stopwatch.StartNew();
            while (true)
            {
                try
                {
                    var reply = await client.GetAsync(new Accounting.Request(),
                        deadline: DateTime.UtcNow.AddSeconds(2), cancellationToken: token);
                    if (reply.Value == expected)
                    {
                        _ = elapsed.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(60));
                        TestContext.Current.TestOutputHelper?.WriteLine($"Received {expected} after {elapsed.Elapsed}");
                        return;
                    }
                }
                catch (RpcException) when (!token.IsCancellationRequested && elapsed.Elapsed < TimeSpan.FromSeconds(58))
                {
                    // Each attempt is a new RPC; no transport retry or in-flight migration is expected.
                }
                _ = elapsed.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(58));
                await Task.Delay(100, token);
            }
        }
    }

    public class GenerationOneServer : Accounting.Invoice.InvoiceBase
    {
        public override Task<Accounting.Reply> Get(Accounting.Request request, ServerCallContext context) =>
            Task.FromResult(new Accounting.Reply { Value = "generation-1" });
    }

    public class GenerationTwoServer : Accounting.Invoice.InvoiceBase
    {
        public override Task<Accounting.Reply> Get(Accounting.Request request, ServerCallContext context) =>
            Task.FromResult(new Accounting.Reply { Value = "generation-2" });
    }
}
