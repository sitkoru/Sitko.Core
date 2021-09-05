using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using IL.FluentValidation.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sitko.Core.App
{
    internal class ApplicationModuleRegistration<TModule, TModuleOptions> : ApplicationModuleRegistration
        where TModule : IApplicationModule<TModuleOptions>, new() where TModuleOptions : BaseModuleOptions, new()
    {
        private readonly string? optionsKey;
        private readonly Action<IConfiguration, IHostEnvironment, TModuleOptions>? configureOptions;
        private readonly TModule instance;
        private readonly Type? validatorType;

        public ApplicationModuleRegistration(
            Action<IConfiguration, IHostEnvironment, TModuleOptions>? configureOptions = null,
            string? optionsKey = null)
        {
            instance = Activator.CreateInstance<TModule>();
            this.configureOptions = configureOptions;
            this.optionsKey = optionsKey ?? instance.OptionsKey;
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

        public override (string? optionsKey, object options) GetOptions(ApplicationContext applicationContext) =>
            (optionsKey, CreateOptions(applicationContext));

        public override ApplicationModuleRegistration ConfigureOptions(ApplicationContext context,
            IServiceCollection services)
        {
            var builder = services.AddOptions<TModuleOptions>()
                .Bind(context.Configuration.GetSection(optionsKey))
                .PostConfigure(
                    options =>
                    {
                        options.Configure(context);
                        configureOptions?.Invoke(context.Configuration, context.Environment, options);
                    });

            if (validatorType is not null)
            {
                builder.Services.AddTransient(validatorType);
                builder.FluentValidate()
                    .With(provider => (IValidator<TModuleOptions>)provider.GetRequiredService(validatorType));
            }

            return this;
        }

        public override ApplicationModuleRegistration ConfigureLogging(
            ApplicationContext context,
            LoggerConfiguration loggerConfiguration)
        {
            if (instance is ILoggingModule<TModuleOptions> loggingModule)
            {
                var options = CreateOptions(context, true);
                loggingModule.ConfigureLogging(context, options, loggerConfiguration);
            }

            return this;
        }

        public override ApplicationModuleRegistration ConfigureHostBuilder(ApplicationContext context,
            IHostBuilder hostBuilder)
        {
            if (instance is IHostBuilderModule<TModuleOptions> hostBuilderModule)
            {
                var options = CreateOptions(context);
                hostBuilderModule.ConfigureHostBuilder(context, hostBuilder, options);
            }

            return this;
        }

        public override ApplicationModuleRegistration ConfigureAppConfiguration(ApplicationContext context,
            IConfigurationBuilder configurationBuilder)
        {
            if (instance is IConfigurationModule<TModuleOptions> configurationModule)
            {
                var options = CreateOptions(context);
                configurationModule.ConfigureAppConfiguration(configurationBuilder,
                    options);
            }

            return this;
        }

        public override (bool isSuccess, IEnumerable<Type> missingModules) CheckRequiredModules(
            ApplicationContext context,
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

        public override bool IsEnabled(ApplicationContext context) => CreateOptions(context).Enabled;

        private TModuleOptions CreateOptions(ApplicationContext applicationContext, bool validateOptions = false)
        {
            var options = Activator.CreateInstance<TModuleOptions>();
            applicationContext.Configuration.Bind(optionsKey, options);
            options.Configure(applicationContext);
            configureOptions?.Invoke(applicationContext.Configuration, applicationContext.Environment, options);
            if (validatorType is not null && validateOptions)
            {
                try
                {
                    if (Activator.CreateInstance(validatorType) is IValidator<TModuleOptions> validator)
                    {
                        var result = validator.Validate(options);
                        if (!result.IsValid)
                        {
                            throw new OptionsValidationException(options.GetType().Name, options.GetType(),
                                result.Errors.Select(e => $"{options.GetType().Name}: {e}"));
                        }
                    }
                }
                catch (TargetInvocationException exception)
                {
                    applicationContext.Logger.LogDebug(exception, "Can't create validator {ValidatorType}: {ErrorText}",
                        validatorType, exception.ToString());
                }
            }

            return options;
        }

        public override ApplicationModuleRegistration ConfigureServices(
            ApplicationContext context,
            IServiceCollection services)
        {
            var options = CreateOptions(context, true);
            instance.ConfigureServices(context, services, options);
            return this;
        }

        public override Task ApplicationStopped(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider) =>
            instance.ApplicationStopped(configuration, environment, serviceProvider);

        public override Task ApplicationStopping(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider) =>
            instance.ApplicationStopping(configuration, environment, serviceProvider);

        public override Task ApplicationStarted(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider) =>
            instance.ApplicationStarted(configuration, environment, serviceProvider);

        public override Task InitAsync(ApplicationContext context, IServiceProvider serviceProvider) =>
            instance.InitAsync(context, serviceProvider);
    }

    public abstract class ApplicationModuleRegistration
    {
        public abstract Type Type { get; }
        public abstract IApplicationModule GetInstance();

        public abstract (string? optionsKey, object options) GetOptions(ApplicationContext applicationContext);

        public abstract ApplicationModuleRegistration ConfigureOptions(ApplicationContext context,
            IServiceCollection services);

        public abstract ApplicationModuleRegistration ConfigureLogging(ApplicationContext context,
            LoggerConfiguration loggerConfiguration);

        public abstract ApplicationModuleRegistration ConfigureServices(ApplicationContext context,
            IServiceCollection services);

        public abstract Task ApplicationStopped(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider);

        public abstract Task ApplicationStopping(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider);

        public abstract Task ApplicationStarted(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider);

        public abstract Task InitAsync(ApplicationContext context, IServiceProvider serviceProvider);

        public abstract ApplicationModuleRegistration ConfigureHostBuilder(ApplicationContext context,
            IHostBuilder hostBuilder);

        public abstract ApplicationModuleRegistration ConfigureAppConfiguration(ApplicationContext context,
            IConfigurationBuilder configurationBuilder);

        public abstract (bool isSuccess, IEnumerable<Type> missingModules) CheckRequiredModules(
            ApplicationContext context,
            Type[] registeredModules);

        public abstract bool IsEnabled(ApplicationContext context);
    }
}
