using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Sitko.Core.App
{
    public class ApplicationLifetimeService : BackgroundService
    {
        private readonly IHostApplicationLifetime hostApplicationLifetime;
        private readonly IServiceProvider serviceProvider;
        private readonly Application application;
        private readonly IConfiguration configuration;
        private readonly IHostEnvironment environment;

        public ApplicationLifetimeService(IHostApplicationLifetime hostApplicationLifetime,
            IServiceProvider serviceProvider, Application application, IConfiguration configuration,
            IHostEnvironment environment)
        {
            this.hostApplicationLifetime = hostApplicationLifetime;
            this.serviceProvider = serviceProvider;
            this.application = application;
            this.configuration = configuration;
            this.environment = environment;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // ReSharper disable once AsyncVoidLambda
            hostApplicationLifetime.ApplicationStarted.Register(async () =>
                await application.OnStarted(configuration, environment, serviceProvider));
            // ReSharper disable once AsyncVoidLambda
            hostApplicationLifetime.ApplicationStopping.Register(async () =>
                await application.OnStopping(configuration, environment, serviceProvider));
            // ReSharper disable once AsyncVoidLambda
            hostApplicationLifetime.ApplicationStopped.Register(async () =>
                await application.OnStopped(configuration, environment, serviceProvider));

            return Task.CompletedTask;
        }
    }
}
