using System.Threading.Tasks;
using Elastic.Apm.Api;

namespace Sitko.Core.Queue.Apm
{
    public class QueueElasticApmMiddleware : BaseQueueMiddleware
    {
        private ITracer? tracer;

        private ITracer? GetTracer()
        {
            if (Elastic.Apm.Agent.IsConfigured)
            {
                return tracer ??= Elastic.Apm.Agent.Tracer;
            }

            return null;
        }

        public override Task<QueuePublishResult> PublishAsync<T>(T message, QueueMessageContext messageContext,
            PublishAsyncDelegate<T>? callback = null)
        {
            var currentTracer = GetTracer();
            if (currentTracer != null)
            {
                var transaction = currentTracer.CurrentTransaction;
                if (transaction == null)
                {
                    return currentTracer.CaptureTransaction($"Publish {message.GetType().FullName}",
                        ApiConstants.TypeExternal, () =>
                        {
                            messageContext.RequestId = currentTracer.CurrentTransaction.OutgoingDistributedTracingData.SerializeToString();
                            return base.PublishAsync(message, messageContext, callback);
                        });
                }

                return transaction.CaptureSpan($"Publish {message.GetType().FullName}", ApiConstants.TypeExternal,
                    () =>
                    {
                        messageContext.RequestId = currentTracer.CurrentTransaction.OutgoingDistributedTracingData.SerializeToString();
                        return base.PublishAsync(message, messageContext, callback);
                    }, "Queue");
            }

            return base.PublishAsync(message, messageContext, callback);
        }

        public override Task<bool> ReceiveAsync<T>(T message, QueueMessageContext messageContext,
            ReceiveAsyncDelegate<T>? callback = null)
        {
            var currentTracer = GetTracer();
            if (currentTracer != null)
            {
                var transaction = currentTracer.CurrentTransaction;
                if (transaction == null)
                {
                    return currentTracer.CaptureTransaction($"Process {message.GetType().FullName}",
                        ApiConstants.TypeRequest, () => base.ReceiveAsync(message, messageContext, callback), DistributedTracingData.TryDeserializeFromString(messageContext.RequestId));
                }

                return transaction.CaptureSpan($"Process {message.GetType().FullName}", ApiConstants.TypeExternal,
                    () => base.ReceiveAsync(message, messageContext, callback), "Queue");
            }

            return base.ReceiveAsync(message, messageContext, callback);
        }
    }
}
