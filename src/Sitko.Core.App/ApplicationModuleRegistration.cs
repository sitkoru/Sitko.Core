using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Sitko.Core.App
{
    public class ApplicationModuleRegistration<TModule, TModuleConfig> : IApplicationModuleRegistration
        where TModule : IApplicationModule<TModuleConfig>
        where TModuleConfig : class
    {
        private readonly Func<IConfiguration, IHostEnvironment, TModuleConfig> _configure;

        public ApplicationModuleRegistration(Func<IConfiguration, IHostEnvironment, TModuleConfig> configure)
        {
            _configure = configure;
        }

        public IApplicationModule CreateModule(IHostEnvironment environment, IConfiguration configuration,
            Application application)
        {
            var config = _configure(configuration, environment);
            var module = (TModule)Activator.CreateInstance(typeof(TModule), config, application);
            return module;
        }
    }
}
