using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Grpc.Core;
using Sitko.Core.App.Web;
using Sitko.Core.Grpc.Server;
using Sitko.Core.ServiceDiscovery.Consul;

namespace Sitko.Core.Grpc.LifecycleHost
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            _ = builder.Logging.ClearProviders();
            using var certificate = X509CertificateLoader.LoadPkcs12FromFile(
                builder.Configuration["Certificate"]!, null);
            _ = builder.WebHost.ConfigureKestrel(options =>
            {
                options.ConfigureEndpointDefaults(listen =>
                {
                    if (listen.IPEndPoint is { } endpoint)
                    {
                        endpoint.Address = IPAddress.Loopback;
                    }
                });
                options.ConfigureHttpsDefaults(https => https.OnAuthenticate = (_, ssl) =>
                {
                    ssl.ServerCertificate = certificate;
                    ssl.ServerCertificateContext = SslStreamCertificateContext.Create(certificate, null);
                });
            });
            var app = builder.AddSitkoCoreWeb();
            _ = app.AddConsulServiceDiscovery();
            _ = app.AddGrpcServer().RegisterService<ReplicaServer>();
            await using var host = builder.Build();
            _ = host.MapSitkoCore();
            await host.StartAsync();
            var port = app.Context.GetModuleInstance<AppWebConfigurationModule>().Listeners["gRPC"].IPEndPoint!.Port;
            Console.WriteLine($"READY:{port}");
            _ = await Console.In.ReadLineAsync();
            await host.StopAsync();
        }
    }

    public class ReplicaServer(IConfiguration configuration) : Contract.Invoice.InvoiceBase
    {
        public override Task<Contract.Reply> Get(Contract.Request request, ServerCallContext context) =>
            Task.FromResult(new Contract.Reply { Value = configuration["Replica"] });
    }
}
