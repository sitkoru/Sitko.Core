using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sitko.Core.App;
using Xunit.Abstractions;

namespace Sitko.Core.Xunit
{
    public abstract class BaseTestScope : IAsyncDisposable
    {
        protected IServiceProvider? ServiceProvider;
        private TestApplication? _application;
        private bool _isApplicationStarted;

        public void Configure(string name, ITestOutputHelper testOutputHelper)
        {
            _application = new TestApplication(new string[0]);

            _application.ConfigureServices((context, services) =>
            {
                services.AddLogging(o => o.AddProvider(new XunitLoggerProvider(testOutputHelper)));
                ConfigureServices(context.Configuration, context.HostingEnvironment, services, name);
            });


            _application = ConfigureApplication(_application, name);
            ServiceProvider = _application.GetServices();
        }

        protected virtual TestApplication ConfigureApplication(TestApplication application, string name)
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

    public class TestApplication : Application<TestApplication>
    {
        public TestApplication(string[] args) : base(args)
        {
        }
    }
}
