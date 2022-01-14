using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Sitko.Core.App;

public interface IApplicationModule<in TModuleOptions> : IApplicationModule where TModuleOptions : class, new()
{
    string OptionsKey { get; }
    bool AllowMultiple { get; }

    void ConfigureServices(IApplicationContext context, IServiceCollection services, TModuleOptions startupOptions);

    IEnumerable<Type> GetRequiredModules(IApplicationContext context, TModuleOptions options);
}

public interface IApplicationModule
{
    Task InitAsync(IApplicationContext context, IServiceProvider serviceProvider);

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

public interface IHostBuilderModule<in TModuleOptions> : IApplicationModule<TModuleOptions>
    where TModuleOptions : class, new()
{
    public void ConfigureHostBuilder(IApplicationContext context, IHostBuilder hostBuilder,
        TModuleOptions startupOptions);
}

public interface ILoggingModule<in TModuleOptions> : IApplicationModule<TModuleOptions>
    where TModuleOptions : class, new()
{
    void ConfigureLogging(IApplicationContext context, TModuleOptions options,
        LoggerConfiguration loggerConfiguration);
}

public interface IConfigurationModule
{
    void CheckConfiguration(IApplicationContext context, IServiceProvider serviceProvider);
}

public interface IConfigurationModule<in TModuleOptions> : IApplicationModule<TModuleOptions>, IConfigurationModule
    where TModuleOptions : class, new()
{
    void ConfigureAppConfiguration(IConfigurationBuilder configurationBuilder,
        TModuleOptions startupOptions);
}
