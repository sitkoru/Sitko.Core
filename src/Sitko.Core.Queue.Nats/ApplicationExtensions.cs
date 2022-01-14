using System;
using Sitko.Core.App;

namespace Sitko.Core.Queue.Nats;

public static class ApplicationExtensions
{
    public static Application AddNatsQueue(this Application application,
        Action<IApplicationContext, NatsQueueModuleOptions> configure, string? optionsKey = null) =>
        application.AddModule<NatsQueueModule, NatsQueueModuleOptions>(configure, optionsKey);

    public static Application AddNatsQueue(this Application application,
        Action<NatsQueueModuleOptions>? configure = null, string? optionsKey = null) =>
        application.AddModule<NatsQueueModule, NatsQueueModuleOptions>(configure, optionsKey);
}
