using System;

namespace Sitko.Core.Queue
{
    public abstract class QueueResult
    {
        public bool IsSuccess { get; protected set; } = true;
        public string ErrorMessage { get; protected set; }
        public Exception Exception { get; protected set; }

        public void SetException(Exception ex)
        {
            IsSuccess = false;
            ErrorMessage = ex.ToString();
            Exception = ex;
        }

        public void SetError(string error)
        {
            IsSuccess = false;
            ErrorMessage = error;
        }
    }

    public class QueueSubscribeResult : QueueResult
    {
        public Guid SubscriptionId { get; set; }
        public IQueueMessageOptions? Options { get; set; }
    }
    
    public class QueuePublishResult:QueueResult
    {
    }
}
