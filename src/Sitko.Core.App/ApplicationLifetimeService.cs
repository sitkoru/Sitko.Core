using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Sitko.Core.App
{
    public class ApplicationLifetimeService : BackgroundService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IServiceProvider _serviceProvider;
        private readonly Application _application;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;

        public ApplicationLifetimeService(IHostApplicationLifetime hostApplicationLifetime,
            IServiceProvider serviceProvider, Application application, IConfiguration configuration,
            IHostEnvironment environment)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _serviceProvider = serviceProvider;
            _application = application;
            _configuration = configuration;
            _environment = environment;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _hostApplicationLifetime.ApplicationStarted.Register(() =>
                _application.OnStarted(_configuration, _environment, _serviceProvider));
            _hostApplicationLifetime.ApplicationStopping.Register(() =>
                _application.OnStopping(_configuration, _environment, _serviceProvider));
            _hostApplicationLifetime.ApplicationStopped.Register(() =>
                _application.OnStopped(_configuration, _environment, _serviceProvider));

            return Task.CompletedTask;
        }
    }
}
