using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.App.Web;

namespace Sitko.Core.Grpc.Server;

public abstract class BaseGrpcServerModule<TConfig> : BaseApplicationModule<TConfig>, IGrpcServerModule,
    IHostBuilderModule<TConfig>,
    IWebApplicationModule where TConfig : GrpcServerModuleOptions, new()
{
    private readonly List<Action<IEndpointRouteBuilder>> endpointRegistrations = new();

    public virtual void RegisterService<TService>() where TService : class =>
        endpointRegistrations.Add(builder => builder.MapGrpcService<TService>());

    public override void ConfigureServices(IApplicationContext context, IServiceCollection services,
        TConfig startupOptions)
    {
        base.ConfigureServices(context, services, startupOptions);
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

    public void ConfigureHostBuilder(IApplicationContext context, IHostBuilder hostBuilder, TConfig startupOptions)
    {
        if (startupOptions.ConfigureWebHostDefaults is not null)
        {
            hostBuilder.ConfigureWebHostDefaults(builder =>
            {
                startupOptions.ConfigureWebHostDefaults(builder);
            });
        }
    }

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
