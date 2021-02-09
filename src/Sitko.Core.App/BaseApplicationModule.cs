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
        protected BaseApplicationModule(BaseApplicationModuleConfig config, Application application) : base(
            config, application)
        {
        }
    }

    public class BaseApplicationModuleConfig
    {
    }

    public abstract class BaseApplicationModule<TConfig> : IApplicationModule<TConfig> where TConfig : class, new()
    {
        protected TConfig Config { get; }
        protected Application Application { get; }

        protected BaseApplicationModule(TConfig config, Application application)
        {
            Config = config;
            Application = application;
        }


        public virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            services.AddSingleton(Config);
        }

        public virtual void ConfigureLogging(LoggerConfiguration loggerConfiguration,
            LogLevelSwitcher logLevelSwitcher, IConfiguration configuration, IHostEnvironment environment)
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

        public virtual void CheckConfig()
        {
        }

        public TConfig GetConfig()
        {
            return Config;
        }
    }
}
