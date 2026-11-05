using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Grpc.Server;
using Xunit;

namespace Sitko.Core.Grpc.Client.Tests
{
    public class GrpcTokenPolicyTest
    {
        [Fact]
        public async Task GlobalTokenPolicyAppliesBeforeAndAfterRegistrationWithTypedOverride()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var certificate = ClientAddressSelectionTest.CreateCertificate();
            var (server, port) = await ClientAddressSelectionTest.StartServer<DefaultTokenAccountingServer>([],
                certificate, false, cancellationToken, static app => app.AddGrpcServer()
                    .RegisterService<DefaultTokenAccountingServer>().RegisterService<DefaultTokenReportsServer>()
                    .RegisterService<SpecificTokenServer>());
            await using var serverLifetime = server;
            var builder = Host.CreateApplicationBuilder();
            var app = builder.AddSitkoCore();
            _ = app.AddGrpcClient<Accounting.Invoice.InvoiceClient>();
            _ = app.ConfigureGrpcClients(options =>
            {
                ConfigureTransport(options, certificate, port);
                _ = options.AddTokenAuth<DefaultTokenProvider>();
            });
            _ = app.AddReportsClientModule();
            _ = app.AddGrpcClient<TestService.TestServiceClient>(static options =>
                options.AddTokenAuth<SpecificTokenProvider>());
            using var host = builder.Build();
            await host.StartAsync(cancellationToken);
            try
            {
                var accounting = host.Services.GetRequiredService<Accounting.Invoice.InvoiceClient>();
                var reports = host.Services.GetRequiredService<Reports.Invoice.InvoiceClient>();
                var specific = host.Services.GetRequiredService<TestService.TestServiceClient>();
                Assert.Equal("default-accounting-ok", (await accounting.GetAsync(new Accounting.Request(),
                    deadline: DateTime.UtcNow.AddSeconds(15), cancellationToken: cancellationToken)).Value);
                Assert.Equal("default-reports-ok", (await reports.GetAsync(new Reports.Request(),
                    deadline: DateTime.UtcNow.AddSeconds(15), cancellationToken: cancellationToken)).Value);
                Assert.Equal("specific-ok", (await specific.RequestAsync(new TestRequest(),
                    deadline: DateTime.UtcNow.AddSeconds(15), cancellationToken: cancellationToken)).Data);
            }
            finally
            {
                await host.StopAsync(cancellationToken);
                await server.StopAsync(cancellationToken);
            }
        }

        [Fact]
        public async Task LongLivedClientUsesCurrentScopedTokenForEveryRpc()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var certificate = ClientAddressSelectionTest.CreateCertificate();
            var (server, port) = await ClientAddressSelectionTest.StartServer<RotatingTokenServer>([],
                certificate, false, cancellationToken);
            await using var serverLifetime = server;
            var builder = Host.CreateApplicationBuilder();
            _ = builder.Services.AddScoped<RotatingTokenProvider>();
            var app = builder.AddSitkoCore();
            _ = app.ConfigureGrpcClients(options =>
            {
                ConfigureTransport(options, certificate, port);
                _ = options.AddTokenAuth<RotatingTokenProvider>();
            });
            _ = app.AddGrpcClient<TestService.TestServiceClient>();
            using var host = builder.Build();
            await host.StartAsync(cancellationToken);
            try
            {
                using var scope = host.Services.CreateScope();
                var provider = scope.ServiceProvider.GetRequiredService<RotatingTokenProvider>();
                var client = scope.ServiceProvider.GetRequiredService<TestService.TestServiceClient>();
                Assert.Equal("first-ok", (await client.RequestAsync(new TestRequest { Data = "first" },
                    deadline: DateTime.UtcNow.AddSeconds(15), cancellationToken: cancellationToken)).Data);
                provider.Token = "Bearer second-token";
                Assert.Equal("second-ok", (await client.RequestAsync(new TestRequest { Data = "second" },
                    deadline: DateTime.UtcNow.AddSeconds(15), cancellationToken: cancellationToken)).Data);
            }
            finally
            {
                await host.StopAsync(cancellationToken);
                await server.StopAsync(cancellationToken);
            }
        }

        private static void ConfigureTransport(GrpcClientModuleOptions options, X509Certificate2 certificate, int port)
        {
            options.Address = new Uri($"https://127.0.0.1:{port}");
            options.ConfigureHttpHandler = handler =>
            {
                ((SocketsHttpHandler)handler).SslOptions.RemoteCertificateValidationCallback = (_, remote, _, _) =>
                    remote is not null && remote.GetCertHashString(HashAlgorithmName.SHA256) ==
                    certificate.GetCertHashString(HashAlgorithmName.SHA256);
                return handler;
            };
        }
    }

    public class RotatingTokenProvider : IGrpcTokenProvider
    {
        public string Token { get; set; } = "Bearer first-token";
        public Task<string> GetTokenAsync() => Task.FromResult(Token);
    }

    public class RotatingTokenServer : TestService.TestServiceBase
    {
        public override Task<TestResponse> Request(TestRequest request, ServerCallContext context)
        {
            if (request.Data == "first")
            {
                Assert.Equal("Bearer first-token", context.RequestHeaders.GetValue("authorization"));
                return Task.FromResult(new TestResponse { Data = "first-ok" });
            }
            Assert.Equal("second", request.Data);
            Assert.Equal("Bearer second-token", context.RequestHeaders.GetValue("authorization"));
            return Task.FromResult(new TestResponse { Data = "second-ok" });
        }
    }

    public class DefaultTokenProvider : IGrpcTokenProvider
    {
        public Task<string> GetTokenAsync() => Task.FromResult("Bearer default-token");
    }

    public class SpecificTokenProvider : IGrpcTokenProvider
    {
        public Task<string> GetTokenAsync() => Task.FromResult("Bearer specific-token");
    }

    public class DefaultTokenAccountingServer : Accounting.Invoice.InvoiceBase
    {
        public override Task<Accounting.Reply> Get(Accounting.Request request, ServerCallContext context)
        {
            Assert.Equal("Bearer default-token", context.RequestHeaders.GetValue("authorization"));
            return Task.FromResult(new Accounting.Reply { Value = "default-accounting-ok" });
        }
    }

    public class DefaultTokenReportsServer : Reports.Invoice.InvoiceBase
    {
        public override Task<Reports.Reply> Get(Reports.Request request, ServerCallContext context)
        {
            Assert.Equal("Bearer default-token", context.RequestHeaders.GetValue("authorization"));
            return Task.FromResult(new Reports.Reply { Value = "default-reports-ok" });
        }
    }

    public class SpecificTokenServer : TestService.TestServiceBase
    {
        public override Task<TestResponse> Request(TestRequest request, ServerCallContext context)
        {
            Assert.Equal("Bearer specific-token", context.RequestHeaders.GetValue("authorization"));
            return Task.FromResult(new TestResponse { Data = "specific-ok" });
        }
    }
}
