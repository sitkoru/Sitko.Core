using System.Collections.Generic;

namespace Sitko.Core.Queue.Internal
{
    public class QueueContext
    {
        public readonly List<IQueueMiddleware> Middleware = new List<IQueueMiddleware>();
        public readonly List<IQueueMessageOptions> MessageOptions = new List<IQueueMessageOptions>();

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
    }
}
