using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.App.Web;
using Sitko.Core.ServiceDiscovery;

namespace Sitko.Core.Grpc.Server;

public abstract class BaseGrpcServerModule<TConfig> : BaseApplicationModule<TConfig>, IGrpcServerModule,
    IHostBuilderModule<TConfig>,
    IWebApplicationModule<TConfig> where TConfig : GrpcServerModuleOptions, new()
{
    private readonly List<Action<IEndpointRouteBuilder>> endpointRegistrations = [];

    public virtual void RegisterService<TService>(string? requiredAuthorizationSchemeName, bool enableGrpcWeb = false)
        where TService : class =>
        endpointRegistrations.Add(builder =>
        {
            var grpcEndpoint = builder.MapGrpcService<TService>();
            if (enableGrpcWeb)
            {
                grpcEndpoint = grpcEndpoint.EnableGrpcWeb();
            }

            if (!string.IsNullOrEmpty(requiredAuthorizationSchemeName))
            {
                grpcEndpoint.RequireAuthorization(new AuthorizeAttribute
                {
                    AuthenticationSchemes = requiredAuthorizationSchemeName,
                });
            }
        });

    public override IEnumerable<Type> GetRequiredModules(IApplicationContext applicationContext, TConfig options) =>
        options.EnableServiceDiscovery ? [typeof(IServiceDiscoveryModule)] : [];

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

        foreach (var (name, registrationAction) in startupOptions.ServiceRegistrations)
        {
            registrationAction(this);
            if (startupOptions.EnableServiceDiscovery)
            {
                services.AddToServiceDiscovery(new ServiceDiscoveryService(GrpcModuleConstants.GrpcServiceDiscoveryType,
                    name, new Dictionary<string, string>(), startupOptions.ServiceDiscoveryPortNames));
            }
        }
    }

    public void ConfigureWebHost(IApplicationContext applicationContext, ConfigureWebHostBuilder webHostBuilder,
        TConfig options) =>
        options.ConfigureWebHostDefaults?.Invoke(webHostBuilder);

    public void ConfigureEndpoints(IApplicationContext applicationContext,
        IApplicationBuilder appBuilder, IEndpointRouteBuilder endpoints)
    {
        var config = GetOptions(appBuilder.ApplicationServices);
        if (config.EnableGrpcWeb)
        {
            appBuilder.UseGrpcWeb();
        }

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
