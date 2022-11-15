using MediatR;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Queue.MediatR;

internal sealed class MediatRTranslator<T> : INotificationHandler<T> where T : class, INotification
{
    private readonly ILogger<MediatRTranslator<T>> logger;
    private readonly IQueue queue;

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

