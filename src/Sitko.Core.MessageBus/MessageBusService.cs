using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.MessageBus
{
    public class MessageBusService : IHostedService
    {
        private readonly IMessageBus _messageBus;
        private readonly ILogger<MessageBusService> _logger;

        public MessageBusService(IMessageBus messageBus, ILogger<MessageBusService> logger)
        {
            _messageBus = messageBus;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting message bus");
            _messageBus.Start(cancellationToken);
            _logger.LogInformation("Message bus started");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping message bus");
            return _messageBus.StopAsync(cancellationToken);
        }
    }
}
