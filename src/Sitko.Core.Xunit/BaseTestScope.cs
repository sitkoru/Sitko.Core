using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sitko.Core.App;
using Xunit.Abstractions;

namespace Sitko.Core.Xunit
{
    public abstract class BaseTestScope : IDisposable
    {
        public void Configure(string name, ITestOutputHelper testOutputHelper)
        {
            var application = new Application(new string[0]);

            application.ConfigureServices((context, services) =>
            {
                services.AddLogging(o => o.AddProvider(new XunitLoggerProvider(testOutputHelper)));
                ConfigureServices(context.Configuration, context.HostingEnvironment, services, name);
            });


            application = ConfigureApplication(application, name);
            ServiceProvider = application.GetServices();
        }

        protected virtual Application ConfigureApplication(Application application, string name)
        {
            return application;
        }

        protected virtual IServiceCollection ConfigureServices(IConfiguration configuration,
            IHostEnvironment environment, IServiceCollection services, string name)
        {
            return services;
        }


        protected IServiceProvider? ServiceProvider;


        public T Get<T>()
        {
            return ServiceProvider.GetRequiredService<T>();
        }

        public ILogger<T> GetLogger<T>()
        {
            return ServiceProvider.GetRequiredService<ILogger<T>>();
        }

        public virtual void OnCreated()
        {
        }

        public virtual void Dispose()
        {
        }
    }
}
