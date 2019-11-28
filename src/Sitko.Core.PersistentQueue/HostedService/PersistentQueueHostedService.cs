using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.PersistentQueue.HostedService
{
    public class PersistentQueueHostedService<T> : IHostedService
        where T : IMessage, new()
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PersistentQueueHostedService<T>> _logger;
        private readonly IPersistentQueueConsumer<T> _consumer;

        public PersistentQueueHostedService(IPersistentQueueConsumer<T> consumer,
            IServiceScopeFactory scopeFactory, ILogger<PersistentQueueHostedService<T>> logger)
        {
            _consumer = consumer;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Start consumer of {type}", typeof(T));
            await _consumer.RunAsync(async (message, context) =>
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var processor = scope.ServiceProvider.GetRequiredService<IPersistentQueueMessageProcessor<T>>();
                    return await processor.ProcessAsync(message, context);
                }
            });
            _logger.LogInformation("Consumer of {type} is connected", typeof(T));
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stop hosted service of {type}", typeof(T));
            await Task.CompletedTask;
        }
    }
}
