namespace Sitko.Core.Queue
{
    public class QueuePayload<T> where T : class
    {
        public T Message { get; }
        public QueueMessageContext MessageContext { get; }

        public QueuePayload(T message, QueueMessageContext messageContext = null)
        {
            Message = message;
            MessageContext = messageContext;
        }
    }
}
