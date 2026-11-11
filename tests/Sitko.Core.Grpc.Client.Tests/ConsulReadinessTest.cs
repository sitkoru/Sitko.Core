using System.Net;
using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.Grpc.Server;
using Xunit;

namespace Sitko.Core.Grpc.Client.Tests
{
    public class ConsulReadinessTest
    {
        [Fact]
        public async Task ReadinessWaitsForFirstRegistrationAndTtl()
        {
            var token = TestContext.Current.CancellationToken;
            var requests = Channel.CreateUnbounded<string>();
            var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseRetry = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseSecondTtl = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseLateRetry = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var registrations = 0;
            var passes = 0;
            var stubBuilder = WebApplication.CreateBuilder();
            _ = stubBuilder.WebHost.UseUrls("http://127.0.0.1:0");
            await using var stub = stubBuilder.Build();
            _ = stub.MapPut("/v1/agent/service/register", async () =>
            {
                var count = Interlocked.Increment(ref registrations);
                if (count == 1)
                {
                    await requests.Writer.WriteAsync("first", token);
                    await releaseFirst.Task.WaitAsync(token);
                }
                if (count == 2)
                {
                    await requests.Writer.WriteAsync("retry", token);
                    await releaseRetry.Task.WaitAsync(token);
                }
                if (count == 4)
                {
                    await requests.Writer.WriteAsync("late-retry", token);
                    await releaseLateRetry.Task.WaitAsync(token);
                }
                return Results.Ok();
            });
            _ = stub.MapPut("/v1/agent/check/pass/{id}", async (string id) =>
            {
                var count = Interlocked.Increment(ref passes);
                if (count is 1 or 4)
                {
                    return Results.StatusCode(500);
                }
                if (count == 3)
                {
                    await requests.Writer.WriteAsync("second-ttl", token);
                    await releaseSecondTtl.Task.WaitAsync(token);
                }
                return Results.Ok(id);
            });
            _ = stub.MapPut("/v1/agent/service/deregister/{id}", (string id) => Results.Ok(id));
            await stub.StartAsync(token);
            var address = stub.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var certificate = ClientAddressSelectionTest.CreateCertificate();
            var config = new Dictionary<string, string?>
            {
                ["Application:Environment"] = "readiness-test",
                ["ServiceDiscovery:Consul:Address"] = address,
                ["ServiceDiscovery:Consul:DnsServers:0"] = "127.0.0.1:8600",
                ["ServiceDiscovery:Consul:RegistrationRetryInterval"] = "00:00:00.100",
                ["ServiceDiscovery:Consul:HeartbeatInterval"] = "00:00:00.100"
            };
            var (host, port) = await ClientAddressSelectionTest.StartServer<GenerationOneServer>(config, certificate, true, token, app => app.AddGrpcServer().RegisterService<AccountingServer>().RegisterService<ReportsServer>());
            await using var hostLifetime = host;
            using var client = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true })
            {
                BaseAddress = new Uri($"https://127.0.0.1:{port}"),
                DefaultRequestVersion = HttpVersion.Version20,
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
            };
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeout.CancelAfter(TimeSpan.FromSeconds(15));
                Assert.Equal("first", await requests.Reader.ReadAsync(timeout.Token));
                Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/ready", token)).StatusCode);
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/startup", token)).StatusCode);
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/healthz", token)).StatusCode);
                Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health/all", token)).StatusCode);
                _ = releaseFirst.TrySetResult();
                Assert.Equal("retry", await requests.Reader.ReadAsync(timeout.Token));
                Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/ready", token)).StatusCode);
                _ = releaseRetry.TrySetResult();
                Assert.Equal("second-ttl", await requests.Reader.ReadAsync(timeout.Token));
                Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/ready", token)).StatusCode);
                _ = releaseSecondTtl.TrySetResult();
                Assert.Equal("late-retry", await requests.Reader.ReadAsync(timeout.Token));
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/ready", token)).StatusCode);
            }
            finally
            {
                _ = releaseFirst.TrySetResult();
                _ = releaseRetry.TrySetResult();
                _ = releaseSecondTtl.TrySetResult();
                _ = releaseLateRetry.TrySetResult();
                await host.StopAsync(token);
                await stub.StopAsync(token);
            }
        }
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task NoPublishedContractsDoNotBlockReadiness(bool disabled)
        {
            var token = TestContext.Current.CancellationToken;
            using var certificate = ClientAddressSelectionTest.CreateCertificate();
            var config = new Dictionary<string, string?>
            {
                ["Application:Environment"] = "readiness-test",
                ["ServiceDiscovery:Enabled"] = disabled ? "false" : "true"
            };
            var (host, port) = await ClientAddressSelectionTest.StartServer<GenerationOneServer>(
                config, certificate, true, token, app =>
                {
                    if (disabled)
                    {
                        _ = app.AddGrpcServer().RegisterService<GenerationOneServer>();
                    }
                });
            await using var hostLifetime = host;
            using var client = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true })
            {
                BaseAddress = new Uri($"https://127.0.0.1:{port}"),
                DefaultRequestVersion = HttpVersion.Version20,
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
            };
            try
            {
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/ready", token)).StatusCode);
            }
            finally
            {
                await host.StopAsync(token);
            }
        }

    }

}
