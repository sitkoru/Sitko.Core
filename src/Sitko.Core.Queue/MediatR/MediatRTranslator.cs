using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Queue.MediatR
{
    internal class MediatRTranslator<T> : INotificationHandler<T> where T : class, INotification 
    {
        private readonly IQueue _queue;
        private readonly ILogger<MediatRTranslator<T>> _logger;

        public MediatRTranslator(IQueue queue, ILogger<MediatRTranslator<T>> logger)
        {
            _queue = queue;
            _logger = logger;
        }

        public Task Handle(T request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Redirect message {MessageType} from MedaitR to Queue", typeof(T));
            return _queue.PublishAsync(request);
        }
    }
}
