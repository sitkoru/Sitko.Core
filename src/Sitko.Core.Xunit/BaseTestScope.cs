using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Sitko.Core.App;
using Sitko.Core.App.Logging;
using Xunit.Abstractions;

namespace Sitko.Core.Xunit
{
    public interface IBaseTestScope : IAsyncDisposable
    {
        Task ConfigureAsync(string name, ITestOutputHelper testOutputHelper);
        T Get<T>();
        IEnumerable<T> GetAll<T>();
        ILogger<T> GetLogger<T>();
        Task OnCreatedAsync();
        Task StartApplicationAsync();
    }

    public abstract class BaseTestScope<TApplication> : IBaseTestScope where TApplication : Application
    {
        protected IServiceProvider? ServiceProvider;
        protected IConfiguration? Configuration { get; set; }
        protected IHostEnvironment? Environment { get; set; }
        private TApplication? _application;
        private bool _isApplicationStarted;
        protected string? Name { get; private set; }

        public async Task ConfigureAsync(string name, ITestOutputHelper testOutputHelper)
        {
            Name = name;
            _application = CreateApplication();

            _application.ConfigureServices((_, context, services) =>
            {
                ConfigureServices(context.Configuration, context.HostingEnvironment, services, name);
            });

            _application.ConfigureLogging((_, loggerConfiguration, logLevelSwitcher) =>
            {
                loggerConfiguration.WriteTo.TestOutput(testOutputHelper, levelSwitch: logLevelSwitcher.Switch);
            });

            _application = ConfigureApplication(_application, name);
            var host = await _application.BuildAndInitAsync();
            ServiceProvider = host.Services.CreateScope().ServiceProvider;
            Configuration = ServiceProvider.GetService<IConfiguration>();
            Environment = ServiceProvider.GetService<IHostEnvironment>();
        }

        protected virtual TApplication CreateApplication()
        {
            var app = Activator.CreateInstance(typeof(TApplication), new object[] {new string[0]});
            if (app is TApplication application)
            {
                return application;
            }

            throw new Exception($"Can't create application {typeof(TApplication)}");
        }


        protected virtual TApplication ConfigureApplication(TApplication application, string name)
        {
            return application;
        }

        protected virtual IServiceCollection ConfigureServices(IConfiguration configuration,
            IHostEnvironment environment, IServiceCollection services, string name)
        {
            return services;
        }


        public T Get<T>()
        {
            return ServiceProvider!.GetRequiredService<T>();
        }

        public IEnumerable<T> GetAll<T>()
        {
            return ServiceProvider!.GetServices<T>();
        }

        public ILogger<T> GetLogger<T>()
        {
            return ServiceProvider!.GetRequiredService<ILogger<T>>();
        }

        public virtual Task OnCreatedAsync()
        {
            return Task.CompletedTask;
        }


        public virtual async ValueTask DisposeAsync()
        {
            if (_application != null)
            {
                if (_isApplicationStarted)
                {
                    await _application.StopAsync();
                }

                await _application.DisposeAsync();
            }
        }

        public async Task StartApplicationAsync()
        {
            if (_application != null && !_isApplicationStarted)
            {
                await _application.StartAsync();
                _isApplicationStarted = true;
            }
        }
    }

    public abstract class BaseTestScope : BaseTestScope<TestApplication>
    {
    }

    public class TestApplication : Application
    {
        public TestApplication(string[] args) : base(args)
        {
        }

        protected override void ConfigureLogging(ApplicationContext applicationContext,
            LoggerConfiguration loggerConfiguration,
            LogLevelSwitcher logLevelSwitcher)
        {
            base.ConfigureLogging(applicationContext, loggerConfiguration, logLevelSwitcher);
            logLevelSwitcher.Switch.MinimumLevel = LogEventLevel.Debug;
        }

        protected override void ConfigureHostConfiguration(IConfigurationBuilder configurationBuilder)
        {
            base.ConfigureHostConfiguration(configurationBuilder);
            configurationBuilder.AddEnvironmentVariables();
        }
    }
}
