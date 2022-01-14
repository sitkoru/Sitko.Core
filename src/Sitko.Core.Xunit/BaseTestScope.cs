using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Sitko.Core.App;
using Xunit.Abstractions;

namespace Sitko.Core.Xunit;

public interface IBaseTestScope : IAsyncDisposable
{
    Task ConfigureAsync(string name, ITestOutputHelper testOutputHelper);
    T GetService<T>();
    IEnumerable<T> GetServices<T>();
    ILogger<T> GetLogger<T>();
    Task OnCreatedAsync();
    Task StartApplicationAsync();
}

public abstract class BaseTestScope<TApplication, TConfig> : IBaseTestScope
    where TApplication : HostedApplication where TConfig : BaseTestConfig, new()
{
    private bool isApplicationStarted;
    private TApplication? scopeApplication;
    protected IServiceProvider? ServiceProvider { get; set; }
    [PublicAPI] protected IApplicationContext? ApplicationContext { get; set; }
    [PublicAPI] protected string? Name { get; private set; }

    public TConfig Config => GetService<IOptions<TConfig>>().Value;

    public async Task ConfigureAsync(string name, ITestOutputHelper testOutputHelper)
    {
        Name = name;
        scopeApplication = CreateApplication();

        scopeApplication.ConfigureAppConfiguration((applicationContext, builder) =>
        {
            builder.AddJsonFile("appsettings.json", true);
            builder.AddJsonFile($"appsettings.{applicationContext.EnvironmentName}.json", true);
        });

        scopeApplication.ConfigureServices((context, services) =>
        {
            ConfigureServices(context, services, name);
            services.Configure<TConfig>(context.Configuration.GetSection("Tests"));
        });

        scopeApplication.ConfigureLogging((_, loggerConfiguration) =>
        {
            loggerConfiguration.WriteTo.TestOutput(testOutputHelper,
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}{NewLine}---------{NewLine}{Properties:j}{NewLine}---------");
        });

        scopeApplication = ConfigureApplication(scopeApplication, name);
        ServiceProvider = (await scopeApplication.GetServiceProviderAsync()).CreateScope().ServiceProvider;
        ApplicationContext = ServiceProvider.GetService<IApplicationContext>();
    }


    public T GetService<T>()
    {
#pragma warning disable 8714
        return ServiceProvider!.GetRequiredService<T>();
#pragma warning restore 8714
    }

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

    public TConfig GetConfig(IConfiguration configuration)
    {
        var config = new TConfig();
        configuration.GetSection("Tests").Bind(config);
        return config;
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

    protected virtual IServiceCollection ConfigureServices(IApplicationContext applicationContext,
        IServiceCollection services, string name) =>
        services;

    public IServiceScope CreateScope() => ServiceProvider!.CreateScope();
}

public abstract class BaseTestScope<TApplication> : BaseTestScope<TApplication, BaseTestConfig>
    where TApplication : HostedApplication
{
}

public abstract class BaseTestScope : BaseTestScope<TestApplication, BaseTestConfig>
{
}

public class TestApplication : HostedApplication
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
