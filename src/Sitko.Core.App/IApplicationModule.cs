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
    public interface IApplicationModule<TConfig> : IApplicationModule where TConfig : class, new()
    {
        string GetConfigKey();

        void ConfigureLogging(ApplicationContext context, TConfig config, LoggerConfiguration loggerConfiguration,
            LogLevelSwitcher logLevelSwitcher);

        void ConfigureServices(ApplicationContext context, IServiceCollection services, TConfig startupConfig);

        IEnumerable<Type> GetRequiredModules(ApplicationContext context, TConfig config);
    }

    public interface IApplicationModule
    {
        Task InitAsync(ApplicationContext context, IServiceProvider serviceProvider);

        Task ApplicationStarted(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider);

        Task ApplicationStopping(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider);

        Task ApplicationStopped(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider);
    }
}
