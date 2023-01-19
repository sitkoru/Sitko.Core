namespace Sitko.Core.Queue.Internal;

public class QueueContext
{
    public QueueContext(IEnumerable<IQueueMiddleware>? middleware = default,
        IEnumerable<IQueueMessageOptions>? messageOptions = default)
    {
        if (middleware != null)
        {
            Middleware.AddRange(middleware);
        }

        if (messageOptions != null)
        {
            MessageOptions.AddRange(messageOptions);
        }
    }

    public List<IQueueMiddleware> Middleware { get; } = new();
    public List<IQueueMessageOptions> MessageOptions { get; } = new();
}

