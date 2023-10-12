using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Sitko.Core.App.Localization;
using Sitko.Core.App.Logging;
using Sitko.FluentValidation;
using Tempus;

namespace Sitko.Core.App;

public abstract class Application : IApplication, IAsyncDisposable
{
    private readonly List<ApplicationModuleRegistration> moduleRegistrations =
        new();

    private readonly Dictionary<string, object> store = new();
    private bool disposed;

    protected Application(string[] args)
    {
        Args = args;
        AddModule<CommandsModule>();
    }

    protected List<Action<IApplicationContext, IConfigurationBuilder>>
        AppConfigurationActions { get; } = new();

    protected Dictionary<string, LogEventLevel> LogEventLevels { get; } = new();

    protected List<Func<IApplicationContext, LoggerConfiguration, LoggerConfiguration>>
        LoggerConfigurationActions { get; } = new();

    protected List<Action<IApplicationContext, IServiceCollection>>
        ServicesConfigurationActions { get; } = new();

    protected string[] Args { get; }

    public static string OptionsKey => nameof(Application);

    public Guid Id { get; } = Guid.NewGuid();
    public string Environment => GetApplicationOptions().Environment;

    public string Name => GetApplicationOptions().Name;
    public string Version => GetApplicationOptions().Version;

    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        await DisposeAsync(true);
        GC.SuppressFinalize(this);
        disposed = true;
    }

    protected virtual ValueTask DisposeAsync(bool disposing) => new();

    [PublicAPI]
    public ApplicationOptions GetApplicationOptions() => GetContext().Options;

    protected IReadOnlyList<ApplicationModuleRegistration> GetEnabledModuleRegistrations(IApplicationContext context) =>
        GetEnabledModuleRegistrations<IApplicationModule>(context);

    protected IReadOnlyList<ApplicationModuleRegistration>
        GetEnabledModuleRegistrations<TModule>(IApplicationContext context) where TModule : IApplicationModule =>
        moduleRegistrations
            .Where(r => r.GetInstance() is TModule && r.IsEnabled(context)).ToList();


    protected virtual void ConfigureHostConfiguration(IConfigurationBuilder configurationBuilder)
    {
    }

    protected virtual void ConfigureAppConfiguration(HostBuilderContext context,
        IConfigurationBuilder configurationBuilder)
    {
    }

    protected void RegisterApplicationServices<TApplicationContext>(IApplicationContext applicationContext,
        IServiceCollection services) where TApplicationContext : class, IApplicationContext
    {
        LogInternal("Configure app services");
        services.AddSingleton(typeof(IApplication), this);
        services.AddSingleton(typeof(Application), this);
        services.AddSingleton(GetType(), this);
        services.AddSingleton<IApplicationContext, TApplicationContext>();
        services.AddTransient<IScheduler, Scheduler>();
        services.AddFluentValidationExtensions();
        services.AddTransient(typeof(ILocalizationProvider<>), typeof(LocalizationProvider<>));

        foreach (var moduleRegistration in GetEnabledModuleRegistrations(applicationContext))
        {
            moduleRegistration.ConfigureOptions(applicationContext, services);
            moduleRegistration.ConfigureServices(applicationContext, services);
        }

        foreach (var servicesConfigurationAction in ServicesConfigurationActions)
        {
            servicesConfigurationAction(applicationContext, services);
        }

        foreach (var moduleRegistration in GetEnabledModuleRegistrations(applicationContext))
        {
            moduleRegistration.PostConfigureServices(applicationContext, services);
        }
    }

    protected void ConfigureConfiguration(IApplicationContext appContext, IConfigurationBuilder builder)
    {
        LogInternal("Configure app configuration");
        foreach (var appConfigurationAction in AppConfigurationActions)
        {
            appConfigurationAction(appContext, builder);
        }

        LogInternal("Configure app configuration in modules");
        foreach (var moduleRegistration in GetEnabledModuleRegistrations<IConfigurationModule>(appContext))
        {
            moduleRegistration.ConfigureAppConfiguration(appContext, builder);
        }

        builder.Add(new SerilogDynamicConfigurationSource());
    }

    protected virtual LoggerConfiguration ConfigureLogging(IApplicationContext applicationContext,
        LoggerConfiguration loggerConfiguration)
    {
        foreach (var (key, value) in LogEventLevels)
        {
            loggerConfiguration = loggerConfiguration.MinimumLevel.Override(key, value);
        }

        foreach (var moduleRegistration in GetEnabledModuleRegistrations<ILoggingModule>(applicationContext))
        {
            loggerConfiguration = moduleRegistration.ConfigureLogging(applicationContext, loggerConfiguration);
        }

        foreach (var loggerConfigurationAction in LoggerConfigurationActions)
        {
            loggerConfiguration = loggerConfigurationAction(applicationContext, loggerConfiguration);
        }

        return loggerConfiguration;
    }

    [PublicAPI]
    public Dictionary<string, object> GetModulesOptions() => GetModulesOptions(GetContext());


    private Dictionary<string, object> GetModulesOptions(IApplicationContext applicationContext)
    {
        var modulesOptions = new Dictionary<string, object> { { OptionsKey, applicationContext.Options } };
        foreach (var moduleRegistration in moduleRegistrations)
        {
            var (configKey, options) = moduleRegistration.GetOptions(applicationContext);
            if (!string.IsNullOrEmpty(configKey))
            {
                var current = modulesOptions;
                var parts = configKey.Split(':');
                for (var i = 0; i < parts.Length; i++)
                {
                    if (i == parts.Length - 1)
                    {
                        current[parts[i]] =
                            JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(options))!;
                    }
                    else
                    {
                        if (current.TryGetValue(parts[i], out var value))
                        {
                            current = (Dictionary<string, object>)value;
                        }
                        else
                        {
                            var part = new Dictionary<string, object>();
                            current[parts[i]] = part;
                            current = part;
                        }
                    }
                }
            }
        }

        return modulesOptions;
    }

    public async Task RunAsync()
    {
        LogInternal("Run app start");
        LogInternal("Build and init");
        var context = await BuildAppContextAsync();

        var enabledModules = GetEnabledModuleRegistrations(context).ToArray();
        foreach (var enabledModule in enabledModules)
        {
            var shouldContinue = await enabledModule.GetInstance().OnBeforeRunAsync(this, context, Args);
            if (!shouldContinue)
            {
                return;
            }
        }

        LogInternal("Check required modules");
        var modulesCheckSuccess = true;
        foreach (var registration in enabledModules)
        {
            var result =
                registration.CheckRequiredModules(context,
                    enabledModules.Select(r => r.Type).ToArray());
            if (!result.isSuccess)
            {
                foreach (var missingModule in result.missingModules)
                {
                    LogInternal($"Required module {missingModule} for module {registration.Type} is not registered");
                }

                modulesCheckSuccess = false;
            }
        }

        if (!modulesCheckSuccess)
        {
            LogInternal("Check required modules failed");
            return;
        }

        foreach (var enabledModule in enabledModules)
        {
            var shouldContinue = await enabledModule.GetInstance().OnAfterRunAsync(this, context, Args);
            if (!shouldContinue)
            {
                return;
            }
        }

        await DoRunAsync();
    }

    protected abstract Task DoRunAsync();

    protected abstract Task<IApplicationContext> BuildAppContextAsync();

    public abstract Task StopAsync();

    protected async Task InitAsync(IServiceProvider serviceProvider)
    {
        LogInternal("Build and init async start");
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Application>>();
        logger.LogInformation("Init modules");
        var registrations = GetEnabledModuleRegistrations(GetContext(scope.ServiceProvider));
        var context = GetContext(scope.ServiceProvider);
        foreach (var configurationModule in registrations.Select(module => module.GetInstance())
                     .OfType<IConfigurationModule>())
        {
            configurationModule.CheckConfiguration(context, scope.ServiceProvider);
        }

        foreach (var registration in registrations)
        {
            logger.LogInformation("Init module {Module}", registration.Type);
            await registration.InitAsync(context, scope.ServiceProvider);
        }

        LogInternal("Build and init async done");
    }

    protected virtual void LogInternal(string message) { }

    protected abstract bool CanAddModule();

    [PublicAPI]
    protected void RegisterModule<TModule, TModuleOptions>(
        Action<IApplicationContext, TModuleOptions>? configureOptions = null,
        string? optionsKey = null)
        where TModule : IApplicationModule<TModuleOptions>, new() where TModuleOptions : BaseModuleOptions, new()
    {
        if (!CanAddModule())
        {
            throw new InvalidOperationException("App host is already built. Can't add modules after it");
        }

        var instance = new TModule();
        if (!instance.AllowMultiple && HasModule<TModule>())
        {
            throw new InvalidOperationException($"Module {typeof(TModule)} already registered");
        }

        moduleRegistrations.Add(
            new ApplicationModuleRegistration<TModule, TModuleOptions>(instance, configureOptions, optionsKey));
    }

    protected virtual void InitApplication()
    {
    }


    [PublicAPI]
    protected abstract IApplicationContext GetContext();

    [PublicAPI]
    protected abstract IApplicationContext GetContext(IServiceProvider serviceProvider);

    public async Task OnStarted(IApplicationContext applicationContext, IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Application>>();
        await OnStartedAsync(applicationContext, serviceProvider);
        foreach (var moduleRegistration in GetEnabledModuleRegistrations(GetContext(serviceProvider)))
        {
            try
            {
                await moduleRegistration.ApplicationStarted(applicationContext, serviceProvider);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error on application started hook in module {Module}: {ErrorText}",
                    moduleRegistration.Type,
                    ex.ToString());
            }
        }
    }

    protected virtual Task OnStartedAsync(IApplicationContext applicationContext,
        IServiceProvider serviceProvider) =>
        Task.CompletedTask;

    public async Task OnStopping(IApplicationContext applicationContext,
        IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Application>>();
        await OnStoppingAsync(applicationContext, serviceProvider);
        foreach (var moduleRegistration in GetEnabledModuleRegistrations(GetContext(serviceProvider)))
        {
            try
            {
                await moduleRegistration.ApplicationStopping(applicationContext, serviceProvider);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error on application stopping hook in module {Module}: {ErrorText}",
                    moduleRegistration.Type,
                    ex.ToString());
            }
        }
    }

    protected virtual Task OnStoppingAsync(IApplicationContext applicationContext,
        IServiceProvider serviceProvider) =>
        Task.CompletedTask;

    public async Task OnStopped(IApplicationContext applicationContext,
        IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Application>>();
        await OnStoppedAsync(applicationContext, serviceProvider);
        foreach (var moduleRegistration in GetEnabledModuleRegistrations(GetContext(serviceProvider)))
        {
            try
            {
                await moduleRegistration.ApplicationStopped(applicationContext, serviceProvider);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error on application stopped hook in module {Module}: {ErrorText}",
                    moduleRegistration.Type,
                    ex.ToString());
            }
        }
    }

    protected virtual Task OnStoppedAsync(IApplicationContext applicationContext, IServiceProvider serviceProvider) =>
        Task.CompletedTask;

    public bool HasModule<TModule>() where TModule : IApplicationModule =>
        moduleRegistrations.Any(r => r.Type == typeof(TModule));


    public void Set(string key, object value) => store[key] = value;

    public T Get<T>(string key)
    {
        if (store.TryGetValue(key, out var value))
        {
            return (T)value;
        }

#pragma warning disable 8603
        return default;
#pragma warning restore 8603
    }

    public Application ConfigureLogLevel(string source, LogEventLevel level)
    {
        LogEventLevels.Add(source, level);
        return this;
    }

    public Application ConfigureLogging(Func<IApplicationContext, LoggerConfiguration, LoggerConfiguration> configure)
    {
        LoggerConfigurationActions.Add(configure);
        return this;
    }

    public Application ConfigureServices(
        Action<IApplicationContext, IServiceCollection> configure)
    {
        ServicesConfigurationActions.Add(configure);
        return this;
    }

    public Application ConfigureServices(Action<IServiceCollection> configure)
    {
        ServicesConfigurationActions.Add((_, services) =>
        {
            configure(services);
        });
        return this;
    }

    public Application ConfigureAppConfiguration(
        Action<IApplicationContext, IConfigurationBuilder> configure)
    {
        AppConfigurationActions.Add(configure);
        return this;
    }

    public Application AddModule<TModule>() where TModule : BaseApplicationModule, new()

    {
        RegisterModule<TModule, BaseApplicationModuleOptions>();
        return this;
    }

    public Application AddModule<TModule, TModuleOptions>(
        Action<IApplicationContext, TModuleOptions> configureOptions,
        string? optionsKey = null)
        where TModule : IApplicationModule<TModuleOptions>, new()
        where TModuleOptions : BaseModuleOptions, new()
    {
        RegisterModule<TModule, TModuleOptions>(configureOptions, optionsKey);
        return this;
    }

    public Application AddModule<TModule, TModuleOptions>(
        Action<TModuleOptions>? configureOptions = null,
        string? optionsKey = null)
        where TModule : IApplicationModule<TModuleOptions>, new()
        where TModuleOptions : BaseModuleOptions, new() =>
        AddModule<TModule, TModuleOptions>((_, moduleOptions) =>
        {
            configureOptions?.Invoke(moduleOptions);
        }, optionsKey);
}
