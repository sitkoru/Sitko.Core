using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Sitko.Core.App;
using Sitko.Core.App.Logging;
using Xunit.Abstractions;

namespace Sitko.Core.Xunit
{
    public interface IBaseTestScope : IAsyncDisposable
    {
        void Configure(string name, ITestOutputHelper testOutputHelper);
        T Get<T>();
        IEnumerable<T> GetAll<T>();
        ILogger<T> GetLogger<T>();
        void OnCreated();
        Task StartApplicationAsync();
    }

    public abstract class BaseTestScope<TApplication> : IBaseTestScope where TApplication : Application<TApplication>
    {
        protected IServiceProvider? ServiceProvider;
        private TApplication? _application;
        private bool _isApplicationStarted;

        public void Configure(string name, ITestOutputHelper testOutputHelper)
        {
            _application = (TApplication)Activator.CreateInstance(typeof(TApplication), new object[] {new string[0]});

            _application.AddModule<TestModule, TestModuleConfig>((configuration, environment, moduleConfig) =>
            {
                moduleConfig.TestOutputHelper = testOutputHelper;
            });


            _application = ConfigureApplication(_application, name);
            ServiceProvider = _application.GetServices().CreateScope().ServiceProvider;
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
            return ServiceProvider.GetRequiredService<T>();
        }

        public IEnumerable<T> GetAll<T>()
        {
            return ServiceProvider.GetServices<T>();
        }

        public ILogger<T> GetLogger<T>()
        {
            return ServiceProvider.GetRequiredService<ILogger<T>>();
        }

        public virtual void OnCreated()
        {
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

    public class TestApplication : Application<TestApplication>
    {
        public TestApplication(string[] args) : base(args)
        {
        }
    }

    public class TestModule : BaseApplicationModule<TestModuleConfig>
    {
        public TestModule(TestModuleConfig config, Application application) : base(config, application)
        {
        }

        public override void ConfigureLogging(LoggerConfiguration loggerConfiguration,
            LogLevelSwitcher logLevelSwitcher, string facility,
            IConfiguration configuration, IHostEnvironment environment)
        {
            base.ConfigureLogging(loggerConfiguration, logLevelSwitcher, facility, configuration, environment);
            if (Config.TestOutputHelper != null)
            {
                loggerConfiguration.WriteTo.TestOutput(Config.TestOutputHelper, levelSwitch: logLevelSwitcher.Switch);
            }
        }
    }

    public class TestModuleConfig
    {
        public ITestOutputHelper? TestOutputHelper { get; set; }
    }
}
