using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using Serilog;
using Sitko.Core.App.Helpers;
using Sitko.Core.App.OpenTelemetry;

namespace Sitko.Core.App;

internal sealed class ApplicationModuleRegistration<TModule, TModuleOptions> : ApplicationModuleRegistration
    where TModule : IApplicationModule<TModuleOptions>, new() where TModuleOptions : BaseModuleOptions, new()
{
    private readonly Action<IApplicationContext, TModuleOptions>? configureOptions;
    private readonly TModule instance;
    private readonly string[] optionKeys;

    private readonly Dictionary<Guid, TModuleOptions> optionsCache = new();
    private readonly Type? validatorType;

    public ApplicationModuleRegistration(
        TModule instance,
        Action<IApplicationContext, TModuleOptions>? configureOptions = null,
        string? optionsKey = null)
    {
        this.instance = instance;
        this.configureOptions = configureOptions;
        this.optionKeys = !string.IsNullOrEmpty(optionsKey) ? new[] { optionsKey } : instance.OptionKeys;
        var optionsInstance = Activator.CreateInstance<TModuleOptions>();
        if (optionsInstance is IModuleOptionsWithValidation moduleOptionsWithValidation)
        {
            validatorType = moduleOptionsWithValidation.GetValidatorType();
        }
        else
        {
            validatorType = typeof(TModuleOptions).Assembly.ExportedTypes
                .Where(typeof(IValidator<TModuleOptions>).IsAssignableFrom).FirstOrDefault();
        }
    }

    public override Type Type => typeof(TModule);

    public override IApplicationModule GetInstance() => instance;

    public override (string? optionsKey, object options) GetOptions(IApplicationContext applicationContext) =>
        (optionKeys.Last(), CreateOptions(applicationContext));

    public override ApplicationModuleRegistration ConfigureOptions(IApplicationContext context,
        IServiceCollection services)
    {
        OptionsHelper.AddOptions(context, services, optionKeys, configureOptions, validatorType);

        return this;
    }

    public override LoggerConfiguration ConfigureLogging(IApplicationContext context,
        LoggerConfiguration loggerConfiguration)
    {
        if (instance is not ILoggingModule<TModuleOptions> loggingModule)
        {
            return loggerConfiguration;
        }

        var options = CreateOptions(context, true);
        return loggingModule.ConfigureLogging(context, options, loggerConfiguration);
    }

    public override OpenTelemetryBuilder ConfigureOpenTelemetry(IApplicationContext context,
        OpenTelemetryBuilder builder)
    {
        if (instance is not IOpenTelemetryModule<TModuleOptions> openTelemetryModule)
        {
            return builder;
        }

        var options = CreateOptions(context, true);
        return openTelemetryModule.ConfigureOpenTelemetry(context, options, builder);
    }

    public override ApplicationModuleRegistration ConfigureHostBuilder(IApplicationContext context,
        IHostApplicationBuilder hostBuilder)
    {
        if (instance is IHostBuilderModule<TModuleOptions> hostBuilderModule)
        {
            var options = CreateOptions(context);
            hostBuilderModule.ConfigureHostBuilder(context, hostBuilder, options);
        }

        return this;
    }

    public override ApplicationModuleRegistration PostConfigureHostBuilder(IApplicationContext context,
        IHostApplicationBuilder hostBuilder)
    {
        if (instance is IHostBuilderModule<TModuleOptions> hostBuilderModule)
        {
            var options = CreateOptions(context);
            hostBuilderModule.PostConfigureHostBuilder(context, hostBuilder, options);
        }

        return this;
    }

    public override ApplicationModuleRegistration ConfigureAppConfiguration(IApplicationContext context,
        IConfigurationBuilder configurationBuilder)
    {
        if (instance is IConfigurationModule<TModuleOptions> configurationModule)
        {
            var options = CreateOptions(context);
            configurationModule.ConfigureAppConfiguration(context, configurationBuilder,
                options);
            var envProviders = configurationBuilder.Sources
                .Where(source => source is EnvironmentVariablesConfigurationSource).ToArray();
            foreach (var envProvider in envProviders)
            {
                configurationBuilder.Sources.Remove(envProvider);
                configurationBuilder.Add(envProvider);
            }
        }

        return this;
    }

    public override (bool isSuccess, IEnumerable<Type> missingModules) CheckRequiredModules(
        IApplicationContext context,
        Type[] registeredModules)
    {
        var options = CreateOptions(context);
        var missingModules = new List<Type>();
        foreach (var requiredModule in instance.GetRequiredModules(context, options))
        {
            if (!registeredModules.Any(t => requiredModule.IsAssignableFrom(t)))
            {
                missingModules.Add(requiredModule);
            }
        }

        return (!missingModules.Any(), missingModules);
    }

    public override bool IsEnabled(IApplicationContext context) => CreateOptions(context).Enabled;

    public override void ClearOptionsCache() => optionsCache.Clear();

    private TModuleOptions CreateOptions(IApplicationContext applicationContext, bool validateOptions = false)
    {
        TModuleOptions options;
        if (optionsCache.TryGetValue(applicationContext.Id, out var value))
        {
            options = value;
        }
        else
        {
            options = OptionsHelper.GetOptions(applicationContext, optionKeys, configureOptions);

            optionsCache[applicationContext.Id] = options;
        }

        if (validatorType is not null && validateOptions)
        {
            OptionsHelper.ValidateOptions(applicationContext, options, validatorType);
        }

        return options;
    }

    public override ApplicationModuleRegistration ConfigureServices(
        IApplicationContext context,
        IServiceCollection services)
    {
        var options = CreateOptions(context, true);
        instance.ConfigureServices(context, services, options);
        return this;
    }

    public override ApplicationModuleRegistration PostConfigureServices(IApplicationContext context,
        IServiceCollection services)
    {
        var options = CreateOptions(context, true);
        instance.PostConfigureServices(context, services, options);
        return this;
    }

    public override Task ApplicationStopped(IApplicationContext applicationContext,
        IServiceProvider serviceProvider, CancellationToken cancellationToken = default) =>
        instance.ApplicationStopped(applicationContext, serviceProvider, cancellationToken);

    public override Task ApplicationStopping(IApplicationContext applicationContext,
        IServiceProvider serviceProvider, CancellationToken cancellationToken = default) =>
        instance.ApplicationStopping(applicationContext, serviceProvider, cancellationToken);

    public override Task ApplicationStarted(IApplicationContext applicationContext,
        IServiceProvider serviceProvider, CancellationToken cancellationToken = default) =>
        instance.ApplicationStarted(applicationContext, serviceProvider, cancellationToken);

    public override Task InitAsync(IApplicationContext context, IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default) =>
        instance.InitAsync(context, serviceProvider, cancellationToken);
}

public abstract class ApplicationModuleRegistration
{
    public abstract Type Type { get; }
    public abstract IApplicationModule GetInstance();

    public abstract (string? optionsKey, object options) GetOptions(IApplicationContext applicationContext);

    public abstract ApplicationModuleRegistration ConfigureOptions(IApplicationContext context,
        IServiceCollection services);

    public abstract LoggerConfiguration ConfigureLogging(IApplicationContext context,
        LoggerConfiguration loggerConfiguration);

    public abstract OpenTelemetryBuilder ConfigureOpenTelemetry(IApplicationContext context,
        OpenTelemetryBuilder builder);

    public abstract ApplicationModuleRegistration ConfigureServices(IApplicationContext context,
        IServiceCollection services);

    public abstract ApplicationModuleRegistration PostConfigureServices(IApplicationContext context,
        IServiceCollection services);

    public abstract Task ApplicationStopped(IApplicationContext applicationContext,
        IServiceProvider serviceProvider, CancellationToken cancellationToken = default);

    public abstract Task ApplicationStopping(IApplicationContext applicationContext, IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default);

    public abstract Task ApplicationStarted(IApplicationContext applicationContext,
        IServiceProvider serviceProvider, CancellationToken cancellationToken = default);

    public abstract Task InitAsync(IApplicationContext context, IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default);

    public abstract ApplicationModuleRegistration ConfigureHostBuilder(IApplicationContext context,
        IHostApplicationBuilder hostBuilder);

    public abstract ApplicationModuleRegistration PostConfigureHostBuilder(IApplicationContext context,
        IHostApplicationBuilder hostBuilder);

    public abstract ApplicationModuleRegistration ConfigureAppConfiguration(IApplicationContext context,
        IConfigurationBuilder configurationBuilder);

    public abstract (bool isSuccess, IEnumerable<Type> missingModules) CheckRequiredModules(
        IApplicationContext context,
        Type[] registeredModules);

    public abstract bool IsEnabled(IApplicationContext context);

    public abstract void ClearOptionsCache();
}
