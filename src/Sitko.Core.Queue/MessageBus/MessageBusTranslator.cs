using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Queue.MessageBus
{
    internal class MessageBusTranslator<T> : INotificationHandler<T> where T : class, INotification 
    {
        private readonly IQueue _queue;
        private readonly ILogger<MessageBusTranslator<T>> _logger;

        public MessageBusTranslator(IQueue queue, ILogger<MessageBusTranslator<T>> logger)
        {
            _queue = queue;
            _logger = logger;
        }

        public Task Handle(T request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Redirect message {MessageType} from MessageBus to Queue", typeof(T));
            return _queue.PublishAsync(request);
        }
    }
}
