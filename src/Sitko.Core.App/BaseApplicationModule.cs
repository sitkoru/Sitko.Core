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
    }

    public class BaseApplicationModuleConfig : BaseModuleConfig
    {
    }

    public abstract class BaseApplicationModule<TConfig> : IApplicationModule<TConfig>
        where TConfig : BaseModuleConfig, new()
    {
        public virtual void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TConfig startupConfig)
        {
        }

        public virtual void ConfigureLogging(ApplicationContext context, TConfig config,
            LoggerConfiguration loggerConfiguration,
            LogLevelSwitcher logLevelSwitcher)
        {
        }

        public virtual Task InitAsync(ApplicationContext context, IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }

        public virtual IEnumerable<Type> GetRequiredModules(ApplicationContext context, TConfig config)
        {
            return new Type[0];
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

        public TConfig GetConfig(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<IOptions<TConfig>>().Value;
        }

        public abstract string GetConfigKey();
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
        public void ConfigureHostBuilder(ApplicationContext context, IHostBuilder hostBuilder, TConfig currentConfig);
    }
}
