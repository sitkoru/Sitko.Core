using System.Threading.Tasks;
using Elastic.Apm.Api;

namespace Sitko.Core.Queue.Apm
{
    public class QueueElasticApmMiddleware : BaseQueueMiddleware
    {
        private ITracer? _tracer;

        private ITracer GetTracer()
        {
            return _tracer ??= Elastic.Apm.Agent.Tracer;
        }

        public override Task<QueuePublishResult> PublishAsync<T>(T message, QueueMessageContext messageContext,
            PublishAsyncDelegate<T>? callback = null)
        {
            var transaction = GetTracer().CurrentTransaction;
            if (transaction == null)
            {
                return GetTracer().CaptureTransaction($"Publish {message.GetType().FullName}",
                    ApiConstants.TypeExternal, () => base.PublishAsync(message, messageContext, callback));
            }

            return transaction.CaptureSpan($"Publish {message.GetType().FullName}", ApiConstants.TypeExternal,
                () => base.PublishAsync(message, messageContext, callback), "Queue");
        }

        public override Task<bool> ReceiveAsync<T>(T message, QueueMessageContext messageContext,
            ReceiveAsyncDelegate<T>? callback = null)
        {
            var transaction = GetTracer().CurrentTransaction;
            if (transaction == null)
            {
                return GetTracer().CaptureTransaction($"Process {message.GetType().FullName}",
                    ApiConstants.TypeRequest, () => base.ReceiveAsync(message, messageContext, callback));
            }

            return transaction.CaptureSpan($"Process {message.GetType().FullName}", ApiConstants.TypeExternal,
                () => base.ReceiveAsync(message, messageContext, callback), "Queue");
        }
    }
}
