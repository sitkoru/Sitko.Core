using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Sitko.Core.App.Logging;

namespace Sitko.Core.App
{
    public abstract class BaseApplicationModule : BaseApplicationModule<BaseApplicationModuleConfig>
    {
        protected BaseApplicationModule(Application application) : base(application)
        {
        }
    }

    public class BaseApplicationModuleConfig : BaseModuleConfig
    {
    }

    public abstract class BaseApplicationModule<TConfig> : IApplicationModule<TConfig>
        where TConfig : BaseModuleConfig, new()
    {
        //protected TConfig Config { get; }
        protected Application Application { get; }

        private IOptionsMonitor<TConfig>? _config;

        protected BaseApplicationModule(Application application)
        {
            //Config = config;
            Application = application;
        }


        public virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            //services.AddSingleton(Config);
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

        public TConfig GetConfig()
        {
            _config ??= Application.GetServices().GetService<IOptionsMonitor<TConfig>>();
            if (_config is null)
            {
                throw new Exception($"Module {GetType()} is not configured");
            }

            return _config.CurrentValue;
        }

        public (bool isSuccess, IEnumerable<string> errors) CheckConfig()
        {
            return GetConfig().CheckConfig();
        }
    }

    public abstract class BaseModuleConfig
    {
        public virtual (bool isSuccess, IEnumerable<string> errors) CheckConfig()
        {
            return (true, new string[0]);
        }
    }

    public interface IHostBuilderModule<TConfig> : IApplicationModule<TConfig> where TConfig : class, new()
    {
        public void ConfigureHostBuilder(IHostBuilder hostBuilder, IConfiguration configuration,
            IHostEnvironment environment);
    }
}
