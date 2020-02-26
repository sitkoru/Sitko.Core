using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Sitko.Core.App.Logging;

namespace Sitko.Core.App
{
    public abstract class BaseApplicationModule : BaseApplicationModule<BaseApplicationModuleConfig>
    {
    }

    public class BaseApplicationModuleConfig
    {
    }

    public abstract class BaseApplicationModule<TConfig> : IApplicationModule<TConfig> where TConfig : class
    {
        protected TConfig Config { get; private set; }

        public ApplicationStore ApplicationStore { get; set; }

        public virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
        }

        public virtual void ConfigureLogging(LoggerConfiguration loggerConfiguration,
            LogLevelSwitcher logLevelSwitcher,
            string facility, IConfiguration configuration,
            IHostEnvironment environment)
        {
        }

        public virtual Task InitAsync(IServiceProvider serviceProvider, IConfiguration configuration,
            IHostEnvironment environment)
        {
            return Task.CompletedTask;
        }

        public virtual List<Type> GetRequiredModules()
        {
            return new List<Type>();
        }

        public virtual Task ApplicationStarted(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }

        public virtual Task ApplicationStopping(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }

        public virtual Task ApplicationStopped(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }

        protected virtual void CheckConfig()
        {
        }

        public virtual void Configure(Func<IConfiguration, IHostEnvironment, TConfig> configure,
            IConfiguration configuration, IHostEnvironment environment)
        {
            Config = configure(configuration, environment);
            CheckConfig();
        }

        public TConfig GetConfig()
        {
            return Config;
        }
    }
}
