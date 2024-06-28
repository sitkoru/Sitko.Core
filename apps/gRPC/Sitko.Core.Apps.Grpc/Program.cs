using Sitko.Core.App.Web;
using Sitko.Core.Apps.Grpc;
using Sitko.Core.Apps.Grpc.Services;
using Sitko.Core.Consul;
using Sitko.Core.Grpc.Client;
using Sitko.Core.Grpc.Server;
using Sitko.Core.ServiceDiscovery;
using Sitko.Core.ServiceDiscovery.Resolver.Consul;
using Sitko.Core.ServiceDiscovery.Server.Consul;
using FooService = Sitko.Core.Apps.Grpc.Services.FooService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddSitkoCoreWeb();
builder.AddGrpcServer(options =>
{
    options.RegisterService<GreeterService>();
    options.RegisterService<FooService>();
});
builder
    .AddGrpcClient<Greeter.GreeterClient>(options => options.DisableCertificatesValidation = true)
    .AddGrpcClient<Sitko.Core.Apps.Grpc.FooService.FooServiceClient>(options =>
        options.DisableCertificatesValidation = true);
builder.AddSitkoCoreWeb()
    .AddConsul()
    .AddServiceDiscovery<ConsulServiceDiscoveryRegistrar, ConsulServiceDiscoveryResolver>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapSitkoCore();
app.MapGet("/Test", async context =>
{
    var client = context.RequestServices.GetRequiredService<Sitko.Core.Apps.Grpc.FooService.FooServiceClient>();
    var result = await client.FooAsync(new FooRequest { Bar = "123" });
    await context.Response.WriteAsync(result.Baz);
});
app.Run();
