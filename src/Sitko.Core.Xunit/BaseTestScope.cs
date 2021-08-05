using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Sitko.Core.App;
using Xunit.Abstractions;

namespace Sitko.Core.Xunit
{
    public interface IBaseTestScope : IAsyncDisposable
    {
        Task ConfigureAsync(string name, ITestOutputHelper testOutputHelper);
        T GetService<T>();
        IEnumerable<T> GetServices<T>();
        ILogger<T> GetLogger<T>();
        Task OnCreatedAsync();
        Task StartApplicationAsync();
    }

    public abstract class BaseTestScope<TApplication> : IBaseTestScope where TApplication : Application
    {
        protected IServiceProvider? ServiceProvider { get; set; }
        [PublicAPI]
        protected IConfiguration? Configuration { get; set; }
        [PublicAPI]
        protected IHostEnvironment? Environment { get; set; }
        private TApplication? scopeApplication;
        private bool isApplicationStarted;
        [PublicAPI]
        protected string? Name { get; private set; }

        public async Task ConfigureAsync(string name, ITestOutputHelper testOutputHelper)
        {
            Name = name;
            scopeApplication = CreateApplication();

            scopeApplication.ConfigureServices((_, context, services) =>
            {
                ConfigureServices(context.Configuration, context.HostingEnvironment, services, name);
            });

            scopeApplication.ConfigureLogging((_, loggerConfiguration, logLevelSwitcher) =>
            {
                loggerConfiguration.WriteTo.TestOutput(testOutputHelper, levelSwitch: logLevelSwitcher.Switch);
            });

            scopeApplication = ConfigureApplication(scopeApplication, name);
            var host = await scopeApplication.BuildAndInitAsync();
            ServiceProvider = host.Services.CreateScope().ServiceProvider;
            Configuration = ServiceProvider.GetService<IConfiguration>();
            Environment = ServiceProvider.GetService<IHostEnvironment>();
        }

        protected virtual TApplication CreateApplication()
        {
            var app = Activator.CreateInstance(typeof(TApplication), new object[] { Array.Empty<string>() });
            if (app is TApplication typedApplication)
            {
                return typedApplication;
            }

            throw new InvalidOperationException($"Can't create application {typeof(TApplication)}");
        }


        protected virtual TApplication ConfigureApplication(TApplication application, string name) => application;

        protected virtual IServiceCollection ConfigureServices(IConfiguration configuration,
            IHostEnvironment environment, IServiceCollection services, string name) =>
            services;


        public T GetService<T>()
        {
#pragma warning disable 8714
            return ServiceProvider!.GetRequiredService<T>();
#pragma warning restore 8714
        }

        public IServiceScope CreateScope() => ServiceProvider!.CreateScope();
        public IEnumerable<T> GetServices<T>() => ServiceProvider!.GetServices<T>();

        public ILogger<T> GetLogger<T>() => ServiceProvider!.GetRequiredService<ILogger<T>>();

        public virtual Task OnCreatedAsync() => Task.CompletedTask;


        public virtual async ValueTask DisposeAsync()
        {
            if (scopeApplication != null)
            {
                if (isApplicationStarted)
                {
                    await scopeApplication.StopAsync();
                }

                await scopeApplication.DisposeAsync();
            }
            GC.SuppressFinalize(this);
        }

        public async Task StartApplicationAsync()
        {
            if (scopeApplication != null && !isApplicationStarted)
            {
                await scopeApplication.StartAsync();
                isApplicationStarted = true;
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

        protected override void ConfigureHostConfiguration(IConfigurationBuilder configurationBuilder)
        {
            base.ConfigureHostConfiguration(configurationBuilder);
            configurationBuilder.AddEnvironmentVariables();
        }
    }
}
