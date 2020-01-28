using System.Collections.Generic;

namespace Sitko.Core.Queue.Internal
{
    public class QueueContext
    {
        public readonly List<IQueueMiddleware> Middlewares = new List<IQueueMiddleware>();
        public readonly List<IQueueMessageOptions> MessageOptions = new List<IQueueMessageOptions>();

        public QueueContext(IEnumerable<IQueueMiddleware> middlewares = default,
            IEnumerable<IQueueMessageOptions> messageOptions = default)
        {
            if (middlewares != null)
            {
                Middlewares.AddRange(middlewares);
            }

            if (messageOptions != null)
            {
                MessageOptions.AddRange(messageOptions);
            }
        }
    }
}
