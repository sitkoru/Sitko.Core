using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.App.Web;

namespace Sitko.Core.Grpc.Server;

public abstract class BaseGrpcServerModule<TConfig> : BaseApplicationModule<TConfig>, IGrpcServerModule,
    IHostBuilderModule<TConfig>,
    IWebApplicationModule<TConfig> where TConfig : GrpcServerModuleOptions, new()
{
    private readonly List<Action<IEndpointRouteBuilder>> endpointRegistrations = new();

    public virtual void RegisterService<TService>(string? requiredAuthorizarionSchemeName) where TService : class =>
        endpointRegistrations.Add(builder =>
        {
            var grpcBuidler = builder.MapGrpcService<TService>();
            if (!string.IsNullOrEmpty(requiredAuthorizarionSchemeName))
            {
                grpcBuidler.RequireAuthorization(new AuthorizeAttribute
                {
                    AuthenticationSchemes = requiredAuthorizarionSchemeName
                });
            }
        });

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TConfig startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddScoped(typeof(IGrpcCallProcessor<>), typeof(GrpcCallProcessor<>));
        services.AddGrpc(options =>
        {
            options.EnableDetailedErrors = startupOptions.EnableDetailedErrors;
            startupOptions.ConfigureGrpcService?.Invoke(options);
        });
        if (startupOptions.EnableReflection)
        {
            services.AddGrpcReflection();
        }

        foreach (var registration in startupOptions.ServiceRegistrations)
        {
            registration(this);
        }
    }

    public void ConfigureWebHost(IApplicationContext context, ConfigureWebHostBuilder webHostBuilder,
        TConfig options) =>
        options.ConfigureWebHostDefaults?.Invoke(webHostBuilder);

    public void ConfigureEndpoints(IApplicationContext applicationContext,
        IApplicationBuilder appBuilder, IEndpointRouteBuilder endpoints)
    {
        var config = GetOptions(appBuilder.ApplicationServices);
        foreach (var endpointRegistration in endpointRegistrations)
        {
            endpointRegistration(endpoints);
        }

        endpoints.MapGrpcService<HealthService>();
        if (config.EnableReflection)
        {
            endpoints.MapGrpcReflectionService();
        }
    }
}
