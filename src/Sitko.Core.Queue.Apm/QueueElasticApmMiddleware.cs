using System;
using System.Threading.Tasks;
using Elastic.Apm.Api;

namespace Sitko.Core.Queue.Apm
{
    public class QueueElasticApmMiddleware : IQueueMiddleware
    {
        private ITracer? _tracer;

        private ITracer GetTracer()
        {
            return _tracer ??= Elastic.Apm.Agent.Tracer;
        }

        public Task<QueuePublishResult> PublishAsync<T>(QueuePayload<T> payload,
            Func<QueuePayload<T>, Task<QueuePublishResult>> next) where T : class
        {
            var transaction = GetTracer().CurrentTransaction;
            if (transaction == null)
            {
                return GetTracer().CaptureTransaction($"Publish {payload.Message.GetType().FullName}",
                    ApiConstants.TypeExternal, async () => await next(payload));
            }

            return transaction.CaptureSpan($"Publish {payload.Message.GetType().FullName}", ApiConstants.TypeExternal,
                async () => await next(payload), "Queue");
        }

        public Task<bool> ReceiveAsync<T>(QueuePayload<T> payload, Func<QueuePayload<T>, Task<bool>> next)
            where T : class
        {
            var transaction = GetTracer().CurrentTransaction;
            if (transaction == null)
            {
                return GetTracer().CaptureTransaction($"Process {payload.Message.GetType().FullName}",
                    ApiConstants.TypeRequest, async () => await next(payload));
            }

            return transaction.CaptureSpan($"Process {payload.Message.GetType().FullName}", ApiConstants.TypeExternal,
                async () => await next(payload), "Queue");
        }
    }
}
