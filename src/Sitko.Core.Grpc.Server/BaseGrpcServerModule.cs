using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Sitko.Core.App;
using Sitko.Core.App.Web;
using Sitko.Core.ServiceDiscovery;

namespace Sitko.Core.Grpc.Server;

public abstract class BaseGrpcServerModule<TConfig> : BaseApplicationModule<TConfig>, IGrpcServerModule,
    IHostBuilderModule<TConfig>,
    IWebApplicationModule<TConfig> where TConfig : GrpcServerModuleOptions, new()
{
    private readonly List<Action<IEndpointRouteBuilder>> endpointRegistrations = [];
    private bool enableGrpcWebEndpoints;
    private readonly List<Action<IServiceCollection, IApplicationContext>> serviceOptionsRegistrations = [];

    public virtual void RegisterService<TService>(string? requiredAuthorizationSchemeName, bool enableGrpcWeb = false,
        Action<GrpcServiceOptions>? configure = null)
        where TService : class
    {
        enableGrpcWebEndpoints |= enableGrpcWeb;
        serviceOptionsRegistrations.Add((services, context) =>
            _ = services.AddGrpc().AddServiceOptions<TService>(serviceOptions =>
            {
                serviceOptions.CompressionProviders = [.. serviceOptions.CompressionProviders];
                context.Configuration.GetSection($"Grpc:Server:Contracts:{GrpcServicesHelper.GetServiceName<TService>()}")
                    .Bind(serviceOptions);
                configure?.Invoke(serviceOptions);
            }));
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
    }

    public override IEnumerable<Type> GetRequiredModules(IApplicationContext applicationContext, TConfig options) =>
        [];

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TConfig startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddScoped(typeof(IGrpcCallProcessor<>), typeof(GrpcCallProcessor<>));
        _ = services.AddGrpc(options => ConfigureServiceOptions(applicationContext, startupOptions, options));
        if (startupOptions.EnableReflection)
        {
            services.AddGrpcReflection();
        }

        foreach (var (name, registrationAction) in GetServiceRegistrations(startupOptions))
        {
            registrationAction(this);
            _ = services.AddToServiceDiscovery(new ServiceDiscoveryService(GrpcModuleConstants.GrpcServiceDiscoveryType,
                name, new Dictionary<string, string>(), startupOptions.ServiceDiscoveryPortNames));
        }
        foreach (var configureServiceOptions in serviceOptionsRegistrations)
        {
            configureServiceOptions(services, applicationContext);
        }
    }

    private static void ConfigureServiceOptions(IApplicationContext context, TConfig startupOptions,
        GrpcServiceOptions options)
    {
        context.Configuration.GetSection("Grpc:Server:Default").Bind(options);
        if (startupOptions.EnableDetailedErrors is { } enableDetailedErrors)
        {
            options.EnableDetailedErrors = enableDetailedErrors;
        }
        startupOptions.ConfigureGrpcService?.Invoke(options);
    }

    protected virtual IReadOnlyDictionary<string, Action<IGrpcServerModule>> GetServiceRegistrations(TConfig options) =>
        new Dictionary<string, Action<IGrpcServerModule>>();

    public void ConfigureWebHost(IApplicationContext applicationContext, ConfigureWebHostBuilder webHostBuilder,
        TConfig options) =>
        options.ConfigureWebHostDefaults?.Invoke(webHostBuilder);

    public void ConfigureEndpoints(IApplicationContext applicationContext,
        IApplicationBuilder appBuilder, IEndpointRouteBuilder endpoints)
    {
        var config = GetOptions(appBuilder.ApplicationServices);
        if (enableGrpcWebEndpoints)
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
