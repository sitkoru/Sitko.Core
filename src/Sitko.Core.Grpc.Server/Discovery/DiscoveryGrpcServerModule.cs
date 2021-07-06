using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Server.Discovery
{
    public abstract class DiscoveryGrpcServerModule<TRegistrar, TConfig> : BaseGrpcServerModule<TConfig>
        where TRegistrar : class, IGrpcServicesRegistrar where TConfig : GrpcServerModuleOptions, new()
    {
        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TConfig startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddSingleton<IGrpcServicesRegistrar, TRegistrar>();
            var healthChecksBuilder = services.AddHealthChecks();
            foreach (var healthChecksRegistration in _healthChecksRegistrations)
            {
                healthChecksRegistration(healthChecksBuilder);
            }
        }

        public override async Task ApplicationStarted(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            var registrar = serviceProvider.GetRequiredService<IGrpcServicesRegistrar>();
            foreach (var serviceRegistration in _serviceRegistrations)
            {
                await serviceRegistration(registrar);
            }
        }

        private readonly List<Func<IGrpcServicesRegistrar, Task>> _serviceRegistrations = new();
        private readonly List<Action<IHealthChecksBuilder>> _healthChecksRegistrations = new();

        public override void RegisterService<TService>()
        {
            base.RegisterService<TService>();
            _serviceRegistrations.Add(registrar => registrar.RegisterAsync<TService>());
            _healthChecksRegistrations.Add(healthCheckBuilder =>
                healthCheckBuilder.AddCheck<GrpcServiceHealthCheck<TService>>(
                    $"Grpc service {typeof(TService).BaseType?.DeclaringType?.Name}"));
        }
    }
}
