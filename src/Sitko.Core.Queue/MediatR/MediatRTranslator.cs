using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Queue.MediatR
{
    internal class MediatRTranslator<T> : INotificationHandler<T> where T : class, INotification
    {
        private readonly IQueue queue;
        private readonly ILogger<MediatRTranslator<T>> logger;

        public MediatRTranslator(IQueue queue, ILogger<MediatRTranslator<T>> logger)
        {
            this.queue = queue;
            this.logger = logger;
        }

        public Task Handle(T request, CancellationToken cancellationToken)
        {
            logger.LogDebug("Redirect message {MessageType} from MedaitR to Queue", typeof(T));
            return queue.PublishAsync(request);
        }
    }
}
