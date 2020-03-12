using System;
using System.Threading.Tasks;

namespace Sitko.Core.Queue
{
    public interface IQueueMiddleware
    {
        Task<QueuePublishResult> PublishAsync<T>(QueuePayload<T> payload,
            Func<QueuePayload<T>, Task<QueuePublishResult>> next) where T : class
        {
            return next(payload);
        }

        Task<bool> ReceiveAsync<T>(QueuePayload<T> payload, Func<QueuePayload<T>, Task<bool>> next) where T : class
        {
            return next(payload);
        }
    }
}
