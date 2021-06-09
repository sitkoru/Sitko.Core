using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Sitko.Core.App.Logging;
using IL.FluentValidation.Extensions.Options;

namespace Sitko.Core.App
{
    internal class ApplicationModuleRegistration<TModule, TModuleOptions> : ApplicationModuleRegistration
        where TModule : IApplicationModule<TModuleOptions>, new() where TModuleOptions : BaseModuleOptions, new()
    {
        private readonly string? _configKey;
        private readonly Action<IConfiguration, IHostEnvironment, TModuleOptions>? _configureOptions;
        private readonly TModule _instance;

        public ApplicationModuleRegistration(
            Action<IConfiguration, IHostEnvironment, TModuleOptions>? configureOptions = null,
            string? optionsKey = null)
        {
            _instance = Activator.CreateInstance<TModule>();
            _configureOptions = configureOptions;
            _configKey = optionsKey ?? _instance.GetOptionsKey();
        }

        public override Type Type => typeof(TModule);

        public override IApplicationModule GetInstance()
        {
            return _instance;
        }

        public override ApplicationModuleRegistration ConfigureOptions(ApplicationContext context,
            IServiceCollection services)
        {
            var builder = services.AddOptions<TModuleOptions>()
                .Bind(context.Configuration.GetSection(_configKey))
                .PostConfigure(
                    options =>
                    {
                        _configureOptions?.Invoke(context.Configuration, context.Environment, options);
                    });
            var optionsInstance = Activator.CreateInstance<TModuleOptions>();
            Type? validatorType;
            if (optionsInstance is IModuleOptionsWithValidation moduleOptionsWithValidation)
            {
                validatorType = moduleOptionsWithValidation.GetValidatorType();
            }
            else
            {
                validatorType = typeof(TModuleOptions).Assembly.ExportedTypes
                    .Where(typeof(IValidator<TModuleOptions>).IsAssignableFrom).FirstOrDefault();
            }

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
            LoggerConfiguration loggerConfiguration, LogLevelSwitcher logLevelSwitcher)
        {
            var options = CreateOptions(context);
            _instance.ConfigureLogging(context, options, loggerConfiguration, logLevelSwitcher);
            return this;
        }

        public override ApplicationModuleRegistration ConfigureHostBuilder(ApplicationContext context,
            IHostBuilder hostBuilder)
        {
            if (_instance is IHostBuilderModule<TModuleOptions> hostBuilderModule)
            {
                var options = CreateOptions(context);
                hostBuilderModule.ConfigureHostBuilder(context, hostBuilder, options);
            }

            return this;
        }

        public override ApplicationModuleRegistration ConfigureAppConfiguration(ApplicationContext context,
            HostBuilderContext hostBuilderContext, IConfigurationBuilder configurationBuilder)
        {
            var options = CreateOptions(context);
            _instance.ConfigureAppConfiguration(context, hostBuilderContext, configurationBuilder, options);
            return this;
        }

        public override (bool isSuccess, IEnumerable<Type> missingModules) CheckRequiredModules(
            ApplicationContext context,
            Type[] registeredModules)
        {
            var options = CreateOptions(context);
            var missingModules = new List<Type>();
            foreach (var requiredModule in _instance.GetRequiredModules(context, options))
            {
                if (!registeredModules.Any(t => requiredModule.IsAssignableFrom(t)))
                {
                    missingModules.Add(requiredModule);
                }
            }

            return (!missingModules.Any(), missingModules);
        }

        public override bool IsEnabled(ApplicationContext context)
        {
            return CreateOptions(context).Enabled;
        }

        private TModuleOptions CreateOptions(ApplicationContext applicationContext)
        {
            var options = Activator.CreateInstance<TModuleOptions>();
            applicationContext.Configuration.Bind(_configKey, options);
            _configureOptions?.Invoke(applicationContext.Configuration, applicationContext.Environment, options);
            return options;
        }

        public override ApplicationModuleRegistration ConfigureServices(
            ApplicationContext context,
            IServiceCollection services)
        {
            var options = CreateOptions(context);
            _instance.ConfigureServices(context, services, options);
            return this;
        }

        public override Task ApplicationStopped(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            return _instance.ApplicationStopped(configuration, environment, serviceProvider);
        }

        public override Task ApplicationStopping(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            return _instance.ApplicationStopping(configuration, environment, serviceProvider);
        }

        public override Task ApplicationStarted(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            return _instance.ApplicationStarted(configuration, environment, serviceProvider);
        }

        public override Task InitAsync(ApplicationContext context, IServiceProvider serviceProvider)
        {
            return _instance.InitAsync(context, serviceProvider);
        }
    }

    public abstract class ApplicationModuleRegistration
    {
        public abstract Type Type { get; }
        public abstract IApplicationModule GetInstance();

        public abstract ApplicationModuleRegistration ConfigureOptions(ApplicationContext context,
            IServiceCollection services);

        public abstract ApplicationModuleRegistration ConfigureLogging(ApplicationContext context,
            LoggerConfiguration loggerConfiguration, LogLevelSwitcher logLevelSwitcher);

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
            HostBuilderContext hostBuilderContext, IConfigurationBuilder configurationBuilder);

        public abstract (bool isSuccess, IEnumerable<Type> missingModules) CheckRequiredModules(
            ApplicationContext context,
            Type[] registeredModules);

        public abstract bool IsEnabled(ApplicationContext context);
    }
}
