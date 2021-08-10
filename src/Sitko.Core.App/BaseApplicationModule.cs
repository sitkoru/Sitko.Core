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
    public abstract class BaseApplicationModule : BaseApplicationModule<BaseApplicationModuleOptions>
    {
    }

    public class BaseApplicationModuleOptions : BaseModuleOptions
    {
    }

    public abstract class BaseApplicationModule<TModuleOptions> : IApplicationModule<TModuleOptions>
        where TModuleOptions : BaseModuleOptions, new()
    {
        public virtual void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TModuleOptions startupOptions)
        {
        }

        public virtual void ConfigureAppConfiguration(ApplicationContext context, HostBuilderContext hostBuilderContext,
            IConfigurationBuilder configurationBuilder, TModuleOptions startupOptions)
        {
        }

        public virtual void ConfigureLogging(ApplicationContext context, TModuleOptions options,
            LoggerConfiguration loggerConfiguration,
            LogLevelSwitcher logLevelSwitcher)
        {
        }

        public virtual Task InitAsync(ApplicationContext context, IServiceProvider serviceProvider) =>
            Task.CompletedTask;

        public virtual IEnumerable<Type> GetRequiredModules(ApplicationContext context, TModuleOptions options) =>
            Type.EmptyTypes;

        public virtual Task ApplicationStarted(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider) =>
            Task.CompletedTask;

        public virtual Task ApplicationStopping(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider) =>
            Task.CompletedTask;

        public virtual Task ApplicationStopped(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider) =>
            Task.CompletedTask;

        public TModuleOptions GetOptions(IServiceProvider serviceProvider) =>
            serviceProvider.GetRequiredService<IOptions<TModuleOptions>>().Value;

        public abstract string OptionsKey { get; }
    }

    public interface IModuleOptionsWithValidation
    {
        Type GetValidatorType();
    }

    public abstract class BaseModuleOptions
    {
        public virtual bool Enabled { get; set; } = true;

        public virtual void Configure(ApplicationContext applicationContext)
        {
        }
    }

    public interface IHostBuilderModule<in TModuleOptions> : IApplicationModule<TModuleOptions>
        where TModuleOptions : class, new()
    {
        public void ConfigureHostBuilder(ApplicationContext context, IHostBuilder hostBuilder,
            TModuleOptions startupOptions);
    }
}
