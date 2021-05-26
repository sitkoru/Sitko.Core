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
        TConfig GetConfig();
        string GetConfigKey();
    }

    public interface IApplicationModule
    {
        void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment);

        void ConfigureLogging(LoggerConfiguration loggerConfiguration, LogLevelSwitcher logLevelSwitcher,
            IConfiguration configuration, IHostEnvironment environment);

        Task InitAsync(IServiceProvider serviceProvider, IConfiguration configuration,
            IHostEnvironment environment);

        List<Type> GetRequiredModules();

        Task ApplicationStarted(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider);

        Task ApplicationStopping(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider);

        Task ApplicationStopped(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider);
        
        (bool isSuccess, IEnumerable<string> errors) CheckConfig();
    }
}
