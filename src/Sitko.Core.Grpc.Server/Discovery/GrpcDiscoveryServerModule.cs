using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Server.Discovery
{
    public class GrpcDiscoveryServerModule : GrpcServerModule
    {
        public GrpcDiscoveryServerModule(GrpcServerOptions config, Application application) : base(
            config, application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            var healthChecksBuilder = services.AddHealthChecks();
            foreach (var healthChecksRegistration in _healthChecksRegistrations)
            {
                healthChecksRegistration(healthChecksBuilder);
            }
        }

        public override Task ApplicationStarted(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            var registrar = serviceProvider.GetRequiredService<IGrpcServicesRegistrar>();
            foreach (var serviceRegistration in _serviceRegistrations)
            {
                serviceRegistration(registrar);
            }

            return Task.CompletedTask;
        }

        private readonly List<Action<IGrpcServicesRegistrar>> _serviceRegistrations =
            new List<Action<IGrpcServicesRegistrar>>();

        private readonly List<Action<IHealthChecksBuilder>> _healthChecksRegistrations =
            new List<Action<IHealthChecksBuilder>>();

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
