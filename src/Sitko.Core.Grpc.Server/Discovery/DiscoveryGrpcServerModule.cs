namespace Sitko.Core.Grpc.Server.Discovery
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using App;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    public abstract class DiscoveryGrpcServerModule<TRegistrar, TConfig> : BaseGrpcServerModule<TConfig>
        where TRegistrar : class, IGrpcServicesRegistrar where TConfig : GrpcServerModuleOptions, new()
    {
        private readonly List<Action<IHealthChecksBuilder>> healthChecksRegistrations = new();

        private readonly List<Func<IGrpcServicesRegistrar, Task>> serviceRegistrations = new();

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TConfig startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddSingleton<IGrpcServicesRegistrar, TRegistrar>();
            var healthChecksBuilder = services.AddHealthChecks();
            foreach (var healthChecksRegistration in healthChecksRegistrations)
            {
                healthChecksRegistration(healthChecksBuilder);
            }
        }

        public override async Task ApplicationStarted(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            var registrar = serviceProvider.GetRequiredService<IGrpcServicesRegistrar>();
            foreach (var serviceRegistration in serviceRegistrations)
            {
                await serviceRegistration(registrar);
            }
        }

        public override void RegisterService<TService>()
        {
            base.RegisterService<TService>();
            serviceRegistrations.Add(registrar => registrar.RegisterAsync<TService>());
            healthChecksRegistrations.Add(healthCheckBuilder =>
                healthCheckBuilder.AddCheck<GrpcServiceHealthCheck<TService>>(
                    $"Grpc service {typeof(TService).BaseType?.DeclaringType?.Name}"));
        }
    }
}
