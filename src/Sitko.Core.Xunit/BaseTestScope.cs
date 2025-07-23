using System.Globalization;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    Task BeforeConfiguredAsync(string name);
    Task StartApplicationAsync();
}

public abstract class BaseTestScope<THostApplicationBuilder, TConfig> : IBaseTestScope
    where TConfig : BaseTestConfig, new() where THostApplicationBuilder : IHostApplicationBuilder
{
    private IHost? app;
    private THostApplicationBuilder? hostApplicationBuilder;
    private bool isApplicationStarted;

    private bool isDisposed;
    protected IServiceProvider? ServiceProvider { get; set; }
    protected Guid Id { get; } = Guid.NewGuid();
    [PublicAPI] protected IApplicationContext? ApplicationContext { get; set; }
    [PublicAPI] protected string? Name { get; private set; }

    public TConfig Config => GetService<IOptions<TConfig>>().Value;

    public Task ConfigureAsync(string name, ITestOutputHelper testOutputHelper)
    {
        Name = name;
        hostApplicationBuilder = CreateHostBuilder();
        hostApplicationBuilder.Configuration.AddJsonFile("appsettings.json", true);
        hostApplicationBuilder.Configuration.AddJsonFile(
            $"appsettings.{hostApplicationBuilder.Environment.EnvironmentName}.json", true);

        hostApplicationBuilder.Services.Configure<TConfig>(hostApplicationBuilder.Configuration.GetSection("Tests"));
        ConfigureServices(hostApplicationBuilder, name);

        hostApplicationBuilder.GetSitkoCore()
            .ConfigureLogging((_, loggerConfiguration) =>
            {
                loggerConfiguration = loggerConfiguration.WriteTo.TestOutput(testOutputHelper,
                    outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}{NewLine}---------{NewLine}{Properties:j}{NewLine}---------",
                    formatProvider: CultureInfo.InvariantCulture);
                return loggerConfiguration;
            });

        ConfigureApplication(hostApplicationBuilder, name);
        app = BuildApplication(hostApplicationBuilder);
        ServiceProvider = app.Services.CreateAsyncScope().ServiceProvider;
        ApplicationContext = ServiceProvider.GetService<IApplicationContext>();
        return Task.CompletedTask;
    }

    public T GetService<T>()
    {
#pragma warning disable 8714
        return ServiceProvider!.GetRequiredService<T>();
#pragma warning restore 8714
    }

    public IEnumerable<T> GetServices<T>() => ServiceProvider!.GetServices<T>();

    public ILogger<T> GetLogger<T>() => ServiceProvider!.GetRequiredService<ILogger<T>>();

    public virtual Task BeforeConfiguredAsync(string name) => Task.CompletedTask;

    public virtual async Task OnCreatedAsync()
    {
        if (!isApplicationStarted)
        {
            var applicationLifecycle = ServiceProvider?.GetRequiredService<IApplicationLifecycle>();
            if (applicationLifecycle is not null)
            {
                await applicationLifecycle.StartingAsync(CancellationToken.None);
                await applicationLifecycle.StartedAsync(CancellationToken.None);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!isDisposed)
        {
            await OnDisposeAsync();
            if (app != null)
            {
                if (isApplicationStarted)
                {
                    await app.StopAsync();
                }
                else
                {
                    var applicationLifecycle = ServiceProvider?.GetRequiredService<IApplicationLifecycle>();
                    if (applicationLifecycle is not null)
                    {
                        await applicationLifecycle.StoppingAsync(CancellationToken.None);
                        await applicationLifecycle.StoppedAsync(CancellationToken.None);
                    }
                }

                app.Dispose();
            }

            await OnAfterDisposeAsync();

            GC.SuppressFinalize(this);
            isDisposed = true;
        }
    }

    public async Task StartApplicationAsync()
    {
        if (app != null && !isApplicationStarted)
        {
            await app.StartAsync();
            isApplicationStarted = true;
        }
    }

    protected virtual Task OnDisposeAsync() => Task.CompletedTask;
    protected virtual Task OnAfterDisposeAsync() => Task.CompletedTask;

    public TConfig GetConfig(IConfiguration configuration)
    {
        var config = new TConfig();
        configuration.GetSection("Tests").Bind(config);
        return config;
    }

    protected abstract THostApplicationBuilder CreateHostBuilder();
    protected abstract IHost BuildApplication(THostApplicationBuilder builder);


    protected virtual IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name)
    {
        hostBuilder.Configuration.AddEnvironmentVariables();
        return hostBuilder;
    }

    protected virtual IHostApplicationBuilder ConfigureServices(IHostApplicationBuilder builder, string name) =>
        builder;

    public IServiceScope CreateScope() => ServiceProvider!.CreateScope();
}

public abstract class BaseTestScope<TConfig> : BaseTestScope<HostApplicationBuilder, TConfig>
    where TConfig : BaseTestConfig, new()
{
    protected override HostApplicationBuilder CreateHostBuilder()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddSitkoCore();
        return builder;
    }

    protected override IHost BuildApplication(HostApplicationBuilder builder) => builder.Build();
}

public abstract class BaseTestScope : BaseTestScope<BaseTestConfig>;
