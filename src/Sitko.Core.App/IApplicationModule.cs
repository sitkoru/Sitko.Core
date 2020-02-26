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
    public interface IApplicationModule<TConfig> : IApplicationModule where TConfig : class
    {
        void Configure(Func<IConfiguration, IHostEnvironment, TConfig> configure, IConfiguration configuration,
            IHostEnvironment environment);

        TConfig GetConfig();
    }

    public interface IApplicationModule
    {
        ApplicationStore ApplicationStore { get; set; }

        void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment);

        void ConfigureLogging(LoggerConfiguration loggerConfiguration, LogLevelSwitcher logLevelSwitcher,
            string facility, IConfiguration configuration,
            IHostEnvironment environment);

        Task InitAsync(IServiceProvider serviceProvider, IConfiguration configuration,
            IHostEnvironment environment);

        List<Type> GetRequiredModules();

        Task ApplicationStarted(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider);

        Task ApplicationStopping(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider);

        Task ApplicationStopped(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider);
    }
}
