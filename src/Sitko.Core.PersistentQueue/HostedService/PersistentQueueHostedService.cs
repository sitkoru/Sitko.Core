using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.PersistentQueue.Consumer;

namespace Sitko.Core.PersistentQueue.HostedService
{
    public sealed class PersistentQueueHostedService<T> : IHostedService, System.IDisposable where T : IMessage, new()
    {
        private readonly PersistentQueueConsumerFactory _consumerFactory;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PersistentQueueHostedService<T>> _logger;
        private readonly PersistedQueueHostedServiceOptions<T> _options;
        private PersistentQueueConsumer<T> _consumer;
        private bool _disposed;

        public PersistentQueueHostedService(IOptions<PersistedQueueHostedServiceOptions<T>> options,
            PersistentQueueConsumerFactory consumerFactory,
            IServiceScopeFactory scopeFactory, ILogger<PersistentQueueHostedService<T>> logger)
        {
            _consumerFactory = consumerFactory;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _options = options.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Start consumer of {type}", typeof(T));
            _consumer?.Dispose();
            _consumer = _consumerFactory.GetConsumer(_options);
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
            _logger.LogInformation("Stop consumer of {type}", typeof(T));
            if (_consumer != null)
            {
                await _consumer.StopAsync();
                _consumer.Dispose();
            }

            _logger.LogInformation("Stopped consumer of {type}", typeof(T));
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _consumer?.Dispose();
        }
    }
}
