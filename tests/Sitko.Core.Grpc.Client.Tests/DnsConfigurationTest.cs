using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using AwesomeAssertions;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Sitko.Core.Grpc.Client.Tests
{
    public class DnsConfigurationTest
    {
        [Fact]
        public async Task ConfiguredDnsTimeoutAndAttemptsBoundResolutionBeforeRpcDeadline()
        {
            var token = TestContext.Current.CancellationToken;
            await using var consul = DiscoveryLifecycleTest.CreateConsul();
            await consul.StartAsync(token);
            using var certificate = ClientAddressSelectionTest.CreateCertificate();
            var config = DiscoveryLifecycleTest.Configuration(consul);
            var (server, _) = await ClientAddressSelectionTest.StartServer<RecoveryServer>(config, certificate, true, token);
            await using var serverLifetime = server;
            try
            {
                foreach (var (timeoutMs, attempts) in new[] { (200, 2), (700, 3) })
                {
                    await using var relay = new DnsResponseRelay(IPEndPoint.Parse(config["ServiceDiscovery:Consul:DnsServers:0"]!));
                    var clientConfig = new Dictionary<string, string?>(config)
                    {
                        ["ServiceDiscovery:Consul:DnsServers:0"] = relay.Endpoint.ToString(),
                        ["ServiceDiscovery:Consul:DnsTimeout"] = TimeSpan.FromMilliseconds(timeoutMs).ToString("c", CultureInfo.InvariantCulture),
                        ["ServiceDiscovery:Consul:DnsMaxAttempts"] = attempts.ToString(CultureInfo.InvariantCulture),
                        ["ServiceDiscovery:Consul:DnsMinRetryPeriod"] = "00:00:05",
                        ["ServiceDiscovery:Consul:DnsMaxRetryPeriod"] = "00:00:05"
                    };
                    using var clientHost = DiscoveryLifecycleTest.CreateClient(clientConfig, certificate);
                    await clientHost.StartAsync(token);
                    try
                    {
                        var client = clientHost.Services.GetRequiredService<Accounting.Invoice.InvoiceClient>();
                        var elapsed = Stopwatch.StartNew();
                        var error = await Assert.ThrowsAsync<RpcException>(async () =>
                            await client.GetAsync(new Accounting.Request(), deadline: DateTime.UtcNow.AddSeconds(15), cancellationToken: token));
                        var resolutionDuration = elapsed.Elapsed;
                        _ = token.IsCancellationRequested.Should().BeFalse();
                        _ = error.StatusCode.Should().NotBe(StatusCode.Cancelled).And.NotBe(StatusCode.DeadlineExceeded);
                        var requests = relay.RequestTimes;
                        _ = requests.Should().HaveCount(attempts);
                        _ = relay.ResponsesReceived.Should().Be(attempts, "each held reply came from the real Consul");
                        var expectedDuration = TimeSpan.FromMilliseconds(timeoutMs * attempts);
                        _ = resolutionDuration.Should().BeGreaterThan(expectedDuration * 0.75)
                            .And.BeLessThan(expectedDuration + TimeSpan.FromSeconds(2));
                        for (var i = 1; i < requests.Length; i++)
                        {
                            _ = (requests[i] - requests[i - 1]).Should()
                                .BeGreaterThan(TimeSpan.FromMilliseconds(timeoutMs * 0.75))
                                .And.BeLessThan(TimeSpan.FromMilliseconds(timeoutMs) + TimeSpan.FromSeconds(1));
                        }
                        TestContext.Current.TestOutputHelper?.WriteLine(
                            $"DNS timeout={timeoutMs}ms; attempts={requests.Length}; elapsed={resolutionDuration}; RPC={error.StatusCode}; received real replies={relay.ResponsesReceived}");
                        relay.EnableResponses();
                        await DiscoveryLifecycleTest.WaitForReply(client, "server-ok", token);
                    }
                    finally
                    {
                        await clientHost.StopAsync(token);
                    }
                }
            }
            finally
            {
                await server.StopAsync(token);
            }
        }
    }

    internal sealed class DnsResponseRelay : IAsyncDisposable
    {
        private readonly UdpClient listener = new(new IPEndPoint(IPAddress.Loopback, 0));
        private readonly CancellationTokenSource shutdown = new();
        private readonly ConcurrentQueue<TimeSpan> requestTimes = new();
        private readonly Stopwatch elapsed = Stopwatch.StartNew();
        private readonly Task loop;
        private int responsesEnabled;
        private int responsesReceived;

        public DnsResponseRelay(IPEndPoint upstream) => loop = Forward(upstream);
        public IPEndPoint Endpoint => (IPEndPoint)listener.Client.LocalEndPoint!;
        public TimeSpan[] RequestTimes => [.. requestTimes];
        public int ResponsesReceived => Volatile.Read(ref responsesReceived);
        public void EnableResponses() => Interlocked.Exchange(ref responsesEnabled, 1);

        private async Task Forward(IPEndPoint upstream)
        {
            while (!shutdown.IsCancellationRequested)
            {
                var request = await listener.ReceiveAsync(shutdown.Token);
                requestTimes.Enqueue(elapsed.Elapsed);
                using var socket = new UdpClient(AddressFamily.InterNetwork);
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(shutdown.Token);
                timeout.CancelAfter(TimeSpan.FromSeconds(2));
                _ = await socket.SendAsync(request.Buffer, upstream, timeout.Token);
                var response = await socket.ReceiveAsync(timeout.Token);
                _ = Interlocked.Increment(ref responsesReceived);
                if (Volatile.Read(ref responsesEnabled) != 0)
                {
                    _ = await listener.SendAsync(response.Buffer, request.RemoteEndPoint, shutdown.Token);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await shutdown.CancelAsync();
            try
            {
                await loop;
            }
            catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
            {
                // Closing the owned relay cancels its pending receive.
            }
            finally
            {
                listener.Dispose();
                shutdown.Dispose();
            }
        }
    }
}
