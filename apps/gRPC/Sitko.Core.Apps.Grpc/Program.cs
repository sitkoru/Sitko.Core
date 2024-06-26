using Sitko.Core.App.Web;
using Sitko.Core.Apps.Grpc;
using Sitko.Core.Apps.Grpc.Services;
using Sitko.Core.Consul;
using Sitko.Core.Grpc;
using Sitko.Core.Grpc.Client;
using Sitko.Core.Grpc.Server;
using Sitko.Core.ServiceDiscovery;
using Sitko.Core.ServiceDiscovery.Consul;
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
    .AddModule<ConsulServiceDiscoveryModule, ConsulServiceDiscoveryModuleOptions>(options =>
    {
        options.Hosts.Add(new ServiceDiscoveryHost(GrpcModuleConstants.GrpcServiceDiscoveryType, true, "127.0.0.1",
            7233));
        // options.Hosts.Add(new ServiceDiscoveryHost(GrpcModuleConstants.GrpcServiceDiscoveryType, false, "127.0.0.1",
        //     5255));
    });

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
