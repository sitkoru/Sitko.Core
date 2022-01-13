using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Sitko.Core.App;

public abstract class Application : IApplication, IAsyncDisposable
{
    private readonly List<ApplicationCommand> commands = new();

    private readonly List<ApplicationModuleRegistration> moduleRegistrations =
        new();

    private readonly Dictionary<string, object> store = new();
    private bool disposed;

    protected Application(string[] args)
    {
        Args = args;
        Console.OutputEncoding = Encoding.UTF8;
        var loggerConfiguration = new LoggerConfiguration();
        loggerConfiguration
            .WriteTo.Console(outputTemplate: ApplicationOptions.BaseConsoleLogFormat,
                restrictedToMinimumLevel: LogEventLevel.Debug);
        InternalLogger = new SerilogLoggerFactory(loggerConfiguration.CreateLogger()).CreateLogger<Application>();
        ProcessArguments();
    }

    protected List<Action<ApplicationContext, IConfigurationBuilder>>
        AppConfigurationActions { get; } = new();

    protected Dictionary<string, LogEventLevel> LogEventLevels { get; } = new();

    protected List<Action<ApplicationContext, LoggerConfiguration>>
        LoggerConfigurationActions { get; } = new();

    protected List<Action<ApplicationContext, IServiceCollection>>
        ServicesConfigurationActions { get; } = new();


    internal ApplicationCommand? CurrentCommand { get; private set; }

    protected string[] Args { get; }

    protected virtual string ApplicationOptionsKey => nameof(Application);

    public Guid Id { get; } = Guid.NewGuid();

    protected ILogger<Application> InternalLogger { get; }

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

    private void ProcessArguments()
    {
        commands.Add(new ApplicationCommand("check", true, true, OnAfterRunAsync: () =>
        {
            LogVerbose("Check run is successful. Exit");
            return Task.FromResult(false);
        }));
        commands.Add(new ApplicationCommand("generate-options", true, OnBeforeRunAsync:
            () =>
            {
                InternalLogger.LogInformation("Generate options");

                var modulesOptions = GetModulesOptions(GetContext(new DummyEnvironment(),
                    new ConfigurationRoot(new List<IConfigurationProvider>())));

                InternalLogger.LogInformation("Modules options:");
                InternalLogger.LogInformation("{Options}", JsonSerializer.Serialize(modulesOptions,
                    new JsonSerializerOptions { WriteIndented = true }));
                return Task.FromResult(false);
            }));
        commands.Add(new ApplicationCommand("run"));
        if (Args.Length > 0)
        {
            var commandName = Args[0];
            CurrentCommand = GetCommand(commandName);
            if (CurrentCommand is null)
            {
                throw new ArgumentException($"Unknown command {commandName}. Supported commands: {commands}",
                    nameof(Args));
            }

            InternalLogger.LogInformation("Run command {CommandName}", CurrentCommand.Name);
        }
    }


    private ApplicationCommand? GetCommand(string commandName) =>
        commands.FirstOrDefault(c => c.Name == commandName);


    [PublicAPI]
    public ApplicationOptions GetApplicationOptions() =>
        GetApplicationOptions(GetContext().Environment, GetContext().Configuration);

    [PublicAPI]
    protected ApplicationOptions GetApplicationOptions(IAppEnvironment environment, IConfiguration configuration)
    {
        var options = new ApplicationOptions();
        configuration.Bind(ApplicationOptionsKey, options);
        if (string.IsNullOrEmpty(options.Name))
        {
            options.Name = GetType().Assembly.GetName().Name ?? "App";
        }

        if (string.IsNullOrEmpty(options.Version))
        {
            options.Version = GetType().Assembly.GetName().Version?.ToString() ?? "dev";
        }

        options.EnableConsoleLogging ??= environment.IsDevelopment();

        ConfigureApplicationOptions(environment, configuration, options);
        return options;
    }

    protected virtual void ConfigureApplicationOptions(IAppEnvironment environment, IConfiguration configuration,
        ApplicationOptions options)
    {
    }

    protected IReadOnlyList<ApplicationModuleRegistration>
        GetEnabledModuleRegistrations(ApplicationContext context) => moduleRegistrations
        .Where(r => r.IsEnabled(context)).ToList();

    protected void LogVerbose(string message)
    {
        if (CurrentCommand?.EnableVerboseLogging == true)
        {
            InternalLogger.LogInformation("Check log: {Message}", message);
        }
    }


    protected virtual void ConfigureHostConfiguration(IConfigurationBuilder configurationBuilder)
    {
    }

    protected virtual void ConfigureAppConfiguration(HostBuilderContext context,
        IConfigurationBuilder configurationBuilder)
    {
    }

    protected virtual void ConfigureLogging(ApplicationContext applicationContext,
        LoggerConfiguration loggerConfiguration)
    {
    }

    [PublicAPI]
    public Dictionary<string, object> GetModulesOptions() => GetModulesOptions(GetContext());


    private Dictionary<string, object> GetModulesOptions(ApplicationContext applicationContext)
    {
        var modulesOptions = new Dictionary<string, object>
        {
            {
                ApplicationOptionsKey,
                GetApplicationOptions(applicationContext.Environment, applicationContext.Configuration)
            }
        };
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
                            JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(options));
                    }
                    else
                    {
                        if (current.ContainsKey(parts[i]))
                        {
                            current = (Dictionary<string, object>)current[parts[i]];
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
        if (CurrentCommand?.OnBeforeRunAsync is not null)
        {
            var shouldContinue = await CurrentCommand.OnBeforeRunAsync();
            if (!shouldContinue)
            {
                return;
            }
        }

        LogVerbose("Run app start");
        LogVerbose("Build and init");
        var context = await BuildAppContextAsync();

        InternalLogger.LogInformation("Check required modules");
        var modulesCheckSuccess = true;
        foreach (var registration in GetEnabledModuleRegistrations(context))
        {
            var result =
                registration.CheckRequiredModules(context,
                    GetEnabledModuleRegistrations(context).Select(r => r.Type).ToArray());
            if (!result.isSuccess)
            {
                foreach (var missingModule in result.missingModules)
                {
                    InternalLogger.LogCritical(
                        "Required module {MissingModule} for module {Module} is not registered",
                        missingModule, registration.Type);
                }

                modulesCheckSuccess = false;
            }
        }

        if (!modulesCheckSuccess)
        {
            InternalLogger.LogError("Check required modules failed");
            return;
        }


        if (CurrentCommand?.OnAfterRunAsync is not null)
        {
            var shouldContinue = await CurrentCommand.OnAfterRunAsync();
            if (!shouldContinue)
            {
                return;
            }
        }

        await DoRunAsync();
    }

    protected abstract Task DoRunAsync();

    protected abstract Task<ApplicationContext> BuildAppContextAsync();

    public abstract Task StopAsync();

    protected async Task InitAsync(IServiceProvider serviceProvider)
    {
        LogVerbose("Build and init async start");
        if (CurrentCommand?.IsInitDisabled != true)
        {
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
        }

        LogVerbose("Build and init async done");
    }


    protected abstract bool CanAddModule();

    [PublicAPI]
    protected void RegisterModule<TModule, TModuleOptions>(
        Action<IConfiguration, IAppEnvironment, TModuleOptions>? configureOptions = null,
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
    protected abstract ApplicationContext GetContext();

    [PublicAPI]
    protected abstract ApplicationContext GetContext(IServiceProvider serviceProvider);

    protected ApplicationContext GetContext(IAppEnvironment environment, IConfiguration configuration,
        ILogger<Application>? logger = null)
    {
        var applicationOptions = GetApplicationOptions(environment, configuration);
        return new ApplicationContext(applicationOptions.Name, applicationOptions.Version, environment,
            configuration,
            logger ?? InternalLogger);
    }

    public async Task OnStarted(IConfiguration configuration, IAppEnvironment environment,
        IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Application>>();
        await OnStartedAsync(configuration, environment, serviceProvider);
        foreach (var moduleRegistration in GetEnabledModuleRegistrations(GetContext(serviceProvider)))
        {
            try
            {
                await moduleRegistration.ApplicationStarted(configuration, environment, serviceProvider);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error on application started hook in module {Module}: {ErrorText}",
                    moduleRegistration.Type,
                    ex.ToString());
            }
        }
    }

    protected virtual Task OnStartedAsync(IConfiguration configuration, IAppEnvironment environment,
        IServiceProvider serviceProvider) =>
        Task.CompletedTask;

    public async Task OnStopping(IConfiguration configuration, IAppEnvironment environment,
        IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Application>>();
        await OnStoppingAsync(configuration, environment, serviceProvider);
        foreach (var moduleRegistration in GetEnabledModuleRegistrations(GetContext(serviceProvider)))
        {
            try
            {
                await moduleRegistration.ApplicationStopping(configuration, environment, serviceProvider);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error on application stopping hook in module {Module}: {ErrorText}",
                    moduleRegistration.Type,
                    ex.ToString());
            }
        }
    }

    protected virtual Task OnStoppingAsync(IConfiguration configuration, IAppEnvironment environment,
        IServiceProvider serviceProvider) =>
        Task.CompletedTask;

    public async Task OnStopped(IConfiguration configuration, IAppEnvironment environment,
        IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Application>>();
        await OnStoppedAsync(configuration, environment, serviceProvider);
        foreach (var moduleRegistration in GetEnabledModuleRegistrations(GetContext(serviceProvider)))
        {
            try
            {
                await moduleRegistration.ApplicationStopped(configuration, environment, serviceProvider);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error on application stopped hook in module {Module}: {ErrorText}",
                    moduleRegistration.Type,
                    ex.ToString());
            }
        }
    }

    protected virtual Task OnStoppedAsync(IConfiguration configuration, IAppEnvironment environment,
        IServiceProvider serviceProvider) =>
        Task.CompletedTask;

    public bool HasModule<TModule>() where TModule : IApplicationModule =>
        moduleRegistrations.Any(r => r.Type == typeof(TModule));


    public void Set(string key, object value) => store[key] = value;

    public T Get<T>(string key)
    {
        if (store.ContainsKey(key))
        {
            return (T)store[key];
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

    public Application ConfigureLogging(Action<ApplicationContext, LoggerConfiguration> configure)
    {
        LoggerConfigurationActions.Add(configure);
        return this;
    }

    public Application ConfigureServices(
        Action<ApplicationContext, IServiceCollection> configure)
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
        Action<ApplicationContext, IConfigurationBuilder> configure)
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
        Action<IConfiguration, IAppEnvironment, TModuleOptions> configureOptions,
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
        AddModule<TModule, TModuleOptions>((_, _, moduleOptions) =>
        {
            configureOptions?.Invoke(moduleOptions);
        }, optionsKey);
}

[PublicAPI]
public class ApplicationContext
{
    public ApplicationContext(string name, string version, IAppEnvironment environment,
        IConfiguration configuration, ILogger logger)
    {
        Name = name;
        Version = version;
        Environment = environment;
        Configuration = configuration;
        Logger = logger;
    }

    public string Name { get; }
    public string Version { get; }
    public IAppEnvironment Environment { get; }
    public IConfiguration Configuration { get; }
    public ILogger Logger { get; }
}

public interface IAppEnvironment
{
    string EnvironmentName { get; }
    string ApplicationName { get; }
    bool IsDevelopment();
    bool IsProduction();
}

internal class DummyEnvironment : IAppEnvironment
{
    public string ContentRootPath { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
    public string ApplicationName { get; } = "";
    public string EnvironmentName { get; } = "";
    public bool IsDevelopment() => true;
    public bool IsProduction() => false;
}

internal record ApplicationCommand(string Name, bool IsInitDisabled = false, bool EnableVerboseLogging = false,
    Func<Task<bool>>? OnBeforeRunAsync = null,
    Func<Task<bool>>? OnAfterRunAsync = null);
