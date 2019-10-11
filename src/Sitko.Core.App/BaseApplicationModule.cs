using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sitko.Core.App
{
    public abstract class BaseApplicationModule : IApplicationModule
    {
        public ApplicationStore ApplicationStore { get; set; }

        public virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration,
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

        protected virtual void CheckConfig()
        {
        }
    }

    public abstract class BaseApplicationModule<TConfig> : BaseApplicationModule, IApplicationModule<TConfig>
        where TConfig : class
    {
        protected TConfig Config { get; private set; }

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
