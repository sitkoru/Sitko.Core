using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Sitko.Core.App;

public interface IApplicationModule<in TModuleOptions> : IApplicationModule where TModuleOptions : class, new()
{
    public string[] OptionKeys { get; }
    bool AllowMultiple { get; }

    void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TModuleOptions startupOptions);

    void PostConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TModuleOptions startupOptions);

    IEnumerable<Type> GetRequiredModules(IApplicationContext applicationContext, TModuleOptions options);
}

public interface IApplicationModule
{
    Task InitAsync(IApplicationContext applicationContext, IServiceProvider serviceProvider);

    Task ApplicationStarted(IApplicationContext applicationContext,
        IServiceProvider serviceProvider);

    Task ApplicationStopping(IApplicationContext applicationContext,
        IServiceProvider serviceProvider);

    Task ApplicationStopped(IApplicationContext applicationContext,
        IServiceProvider serviceProvider);

    Task<bool> OnBeforeRunAsync(Application application, IApplicationContext applicationContext,
        string[] args);

    Task<bool> OnAfterRunAsync(Application application, IApplicationContext applicationContext,
        string[] args);
}

public interface IHostBuilderModule : IApplicationModule
{
}

public interface IHostBuilderModule<in TModuleOptions> : IHostBuilderModule, IApplicationModule<TModuleOptions>
    where TModuleOptions : class, new()
{
    public void ConfigureHostBuilder(IApplicationContext context, IHostBuilder hostBuilder,
        TModuleOptions startupOptions)
    {
    }

    public void PostConfigureHostBuilder(IApplicationContext context, IHostBuilder hostBuilder,
        TModuleOptions startupOptions)
    {
    }
}

public interface ILoggingModule : IApplicationModule
{
}

public interface ILoggingModule<in TModuleOptions> : ILoggingModule, IApplicationModule<TModuleOptions>
    where TModuleOptions : class, new()
{
    LoggerConfiguration ConfigureLogging(IApplicationContext context, TModuleOptions options,
        LoggerConfiguration loggerConfiguration);
}

public interface IConfigurationModule : IApplicationModule
{
    void CheckConfiguration(IApplicationContext context, IServiceProvider serviceProvider);
}

public interface IConfigurationModule<in TModuleOptions> : IApplicationModule<TModuleOptions>, IConfigurationModule
    where TModuleOptions : class, new()
{
    void ConfigureAppConfiguration(IConfigurationBuilder configurationBuilder,
        TModuleOptions startupOptions);
}
