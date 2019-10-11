using System;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.PersistentQueue.HostedService;

namespace Sitko.Core.PersistentQueue
{
    public static class ApplicationExtensions
    {
        public static Application ConfigureQueue<TMessage>(this Application application,
            Action<PersistedQueueHostedServiceOptions<TMessage>> configure)
            where TMessage : IMessage, new()
        {
            return application.ConfigureServices(collection =>
            {
                collection.Configure(configure);
            });
        }
    }
}
