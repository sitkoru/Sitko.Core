using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace Sitko.Core.App;

public interface ISitkoCoreApplicationBuilder
{
    IApplicationContext Context { get; }
    ISitkoCoreApplicationBuilder AddModule<TModule>() where TModule : BaseApplicationModule, new();

    ISitkoCoreApplicationBuilder AddModule<TModule, TModuleOptions>(
        Action<IApplicationContext, TModuleOptions> configureOptions,
        string? optionsKey = null)
        where TModule : IApplicationModule<TModuleOptions>, new()
        where TModuleOptions : BaseModuleOptions, new();

    ISitkoCoreApplicationBuilder AddModule<TModule, TModuleOptions>(
        Action<TModuleOptions>? configureOptions = null,
        string? optionsKey = null)
        where TModule : IApplicationModule<TModuleOptions>, new()
        where TModuleOptions : BaseModuleOptions, new();

    bool HasModule<TModule>() where TModule : IApplicationModule;

    ISitkoCoreApplicationBuilder ConfigureLogLevel(string source, LogEventLevel level);

    ISitkoCoreApplicationBuilder ConfigureLogging(
        Func<IApplicationContext, LoggerConfiguration, LoggerConfiguration> configure);

    ISitkoCoreApplicationBuilder ConfigureServices(Action<IServiceCollection> configure);
    ISitkoCoreApplicationBuilder ConfigureServices(Action<IApplicationContext, IServiceCollection> configure);
}
