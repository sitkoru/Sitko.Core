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
    public interface IApplicationModule<in TModuleOptions> : IApplicationModule where TModuleOptions : class, new()
    {
        string GetOptionsKey();

        void ConfigureLogging(ApplicationContext context, TModuleOptions options, LoggerConfiguration loggerConfiguration,
            LogLevelSwitcher logLevelSwitcher);

        void ConfigureServices(ApplicationContext context, IServiceCollection services, TModuleOptions startupOptions);

        IEnumerable<Type> GetRequiredModules(ApplicationContext context, TModuleOptions config);
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
