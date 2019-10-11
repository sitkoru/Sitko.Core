using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        Task InitAsync(IServiceProvider serviceProvider, IConfiguration configuration,
            IHostEnvironment environment);

        List<Type> GetRequiredModules();
    }
}
