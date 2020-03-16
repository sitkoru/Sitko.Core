using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Sitko.Core.App
{
    public class ApplicationModuleRegistration<TModule, TModuleConfig> : IApplicationModuleRegistration
        where TModule : IApplicationModule<TModuleConfig>
        where TModuleConfig : class, new()
    {
        private readonly Action<IConfiguration, IHostEnvironment, TModuleConfig>? _configure;

        public ApplicationModuleRegistration(Action<IConfiguration, IHostEnvironment, TModuleConfig>? configure = null)
        {
            _configure = configure;
        }

        public IApplicationModule CreateModule(IHostEnvironment environment, IConfiguration configuration,
            Application application, bool configure = true)
        {
            var config = new TModuleConfig();
            if (configure)
            {
                _configure?.Invoke(configuration, environment, config);
            }

            var module = (TModule)Activator.CreateInstance(typeof(TModule), config, application);
            if (configure)
            {
                module.CheckConfig();
            }
            return module;
        }
    }
}
