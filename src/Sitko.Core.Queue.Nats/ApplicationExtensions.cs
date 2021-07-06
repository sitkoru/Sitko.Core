using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Queue.Nats
{
    public static class ApplicationExtensions
    {
        public static Application AddNatsQueue(this Application application,
            Action<IConfiguration, IHostEnvironment, NatsQueueModuleOptions> configure, string? optionsKey = null)
        {
            return application.AddModule<NatsQueueModule, NatsQueueModuleOptions>(configure, optionsKey);
        }

        public static Application AddNatsQueue(this Application application,
            Action<NatsQueueModuleOptions>? configure = null, string? optionsKey = null)
        {
            return application.AddModule<NatsQueueModule, NatsQueueModuleOptions>(configure, optionsKey);
        }
    }
}
