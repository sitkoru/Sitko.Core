using System.Threading.Tasks;

namespace Sitko.Core.Queue
{
    public interface IQueueMiddleware
    {
        Task<QueuePublishResult> OnBeforePublishAsync(object message, QueueMessageContext messageContext)
        {
            return Task.FromResult(new QueuePublishResult());
        }

        Task OnAfterPublishAsync(object message, QueueMessageContext messageContext)
        {
            return Task.CompletedTask;
        }

        Task<bool> OnBeforeReceiveAsync(object message, QueueMessageContext messageContext)
        {
            return Task.FromResult(true);
        }

        Task OnAfterReceiveAsync(object message, QueueMessageContext messageContext)
        {
            return Task.CompletedTask;
        }
    }
}
