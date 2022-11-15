using Elastic.Apm;
using Elastic.Apm.Api;

namespace Sitko.Core.Queue.Apm;

public class QueueElasticApmMiddleware : BaseQueueMiddleware
{
    private ITracer? tracer;

    private ITracer? GetTracer()
    {
        if (Agent.IsConfigured)
        {
            return tracer ??= Agent.Tracer;
        }

        return null;
    }

    public override Task<QueuePublishResult> PublishAsync<T>(T message, QueueMessageContext messageContext,
        Func<T, QueueMessageContext, Task<QueuePublishResult>>? callback = null)
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
                        messageContext.RequestId = currentTracer.CurrentTransaction.OutgoingDistributedTracingData
                            .SerializeToString();
                        return base.PublishAsync(message, messageContext, callback);
                    });
            }

            return transaction.CaptureSpan($"Publish {message.GetType().FullName}", ApiConstants.TypeExternal,
                () =>
                {
                    messageContext.RequestId =
                        currentTracer.CurrentTransaction.OutgoingDistributedTracingData.SerializeToString();
                    return base.PublishAsync(message, messageContext, callback);
                }, "Queue");
        }

        return base.PublishAsync(message, messageContext, callback);
    }

    public override Task<bool> ReceiveAsync<T>(T message, QueueMessageContext messageContext,
        Func<T, QueueMessageContext, Task<bool>>? callback = null)
    {
        var currentTracer = GetTracer();
        if (currentTracer != null)
        {
            var transaction = currentTracer.CurrentTransaction;
            if (transaction == null)
            {
                return currentTracer.CaptureTransaction($"Process {message.GetType().FullName}",
                    ApiConstants.TypeRequest, () => base.ReceiveAsync(message, messageContext, callback),
                    DistributedTracingData.TryDeserializeFromString(messageContext.RequestId));
            }

            return transaction.CaptureSpan($"Process {message.GetType().FullName}", ApiConstants.TypeExternal,
                () => base.ReceiveAsync(message, messageContext, callback), "Queue");
        }

        return base.ReceiveAsync(message, messageContext, callback);
    }
}

