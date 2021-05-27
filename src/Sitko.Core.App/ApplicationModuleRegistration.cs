using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Sitko.Core.App.Logging;

namespace Sitko.Core.App
{
    internal class ApplicationModuleRegistration<TModule, TModuleConfig> : ApplicationModuleRegistration
        where TModule : IApplicationModule<TModuleConfig>, new() where TModuleConfig : BaseModuleConfig, new()
    {
        private readonly Action<IConfiguration, IHostEnvironment, TModuleConfig>? _configure;
        private readonly string? _configKey;
        private readonly TModule _instance;

        public ApplicationModuleRegistration(Action<IConfiguration, IHostEnvironment, TModuleConfig>? configure = null,
            string? configKey = null)
        {
            _instance = Activator.CreateInstance<TModule>();
            _configure = configure;
            _configKey = configKey ?? _instance.GetConfigKey();
        }

        public override IApplicationModule GetInstance()
        {
            return _instance;
        }

        public override ApplicationModuleRegistration Configure(ApplicationContext context,
            IServiceCollection services)
        {
            services.Configure<TModuleConfig>(context.Configuration.GetSection(_configKey))
                .PostConfigure<TModuleConfig>(
                    config =>
                    {
                        _configure?.Invoke(context.Configuration, context.Environment, config);
                    });
            return this;
        }

        public override ApplicationModuleRegistration ConfigureLogging(
            ApplicationContext context,
            LoggerConfiguration loggerConfiguration, LogLevelSwitcher logLevelSwitcher)
        {
            var config = CreateConfig(context.Configuration, context.Environment);
            _instance.ConfigureLogging(context, config, loggerConfiguration, logLevelSwitcher);
            return this;
        }

        public override ApplicationModuleRegistration ConfigureHostBuilder(ApplicationContext context,
            IHostBuilder hostBuilder)
        {
            if (_instance is IHostBuilderModule<TModuleConfig> hostBuilderModule)
            {
                var config = CreateConfig(context.Configuration, context.Environment);
                hostBuilderModule.ConfigureHostBuilder(context, hostBuilder, config);
            }

            return this;
        }

        public override (bool isSuccess, IEnumerable<Type> missingModules) CheckRequiredModules(
            ApplicationContext context,
            Type[] registeredModules)
        {
            var config = CreateConfig(context.Configuration, context.Environment);
            var missingModules = new List<Type>();
            foreach (var requiredModule in _instance.GetRequiredModules(context, config))
            {
                if (!registeredModules.Any(t => requiredModule.IsAssignableFrom(t)))
                {
                    missingModules.Add(requiredModule);
                }
            }

            return (!missingModules.Any(), missingModules);
        }

        private TModuleConfig CreateConfig(IConfiguration configuration, IHostEnvironment environment)
        {
            var config = Activator.CreateInstance<TModuleConfig>();
            configuration.Bind(_configKey, config);
            _configure?.Invoke(configuration, environment, config);
            return config;
        }

        public override ApplicationModuleRegistration ConfigureServices(
            ApplicationContext context,
            IServiceCollection services)
        {
            var config = CreateConfig(context.Configuration, context.Environment);
            _instance.ConfigureServices(context, services, config);
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

        public override (bool isSuccess, IEnumerable<string> errors) CheckConfig(IServiceProvider serviceProvider)
        {
            var config = serviceProvider.GetRequiredService<IOptions<TModuleConfig>>();
            return config.Value.CheckConfig();
        }
    }

    internal abstract class ApplicationModuleRegistration
    {
        public abstract IApplicationModule GetInstance();

        public abstract ApplicationModuleRegistration Configure(ApplicationContext context,
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

        public abstract (bool isSuccess, IEnumerable<string> errors) CheckConfig(IServiceProvider serviceProvider);

        public abstract ApplicationModuleRegistration ConfigureHostBuilder(ApplicationContext context,
            IHostBuilder hostBuilder);

        public abstract (bool isSuccess, IEnumerable<Type> missingModules) CheckRequiredModules(
            ApplicationContext context,
            Type[] registeredModules);
    }
}
