using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Elastic.Apm.Api;

namespace Sitko.Core.Queue.Apm
{
    public class QueueElasticApmMiddleware : IQueueMiddleware
    {
        private readonly ConcurrentDictionary<Guid, ISpan> _messageSpans = new ConcurrentDictionary<Guid, ISpan>();

        private readonly ConcurrentDictionary<Guid, ITransaction> _transactions =
            new ConcurrentDictionary<Guid, ITransaction>();

        public Task<QueuePublishResult> OnBeforePublishAsync(object message, QueueMessageContext messageContext)
        {
            var transaction = Elastic.Apm.Agent.Tracer.CurrentTransaction ??
                              Elastic.Apm.Agent.Tracer.StartTransaction("QueuePublish", ApiConstants.TypeExternal);

            var span = transaction.StartSpan("PublishMessage", ApiConstants.TypeExternal, "Queue");
            _messageSpans.TryAdd(messageContext.Id, span);
            return Task.FromResult(new QueuePublishResult());
        }

        public Task OnAfterPublishAsync(object message, QueueMessageContext messageContext)
        {
            if (_messageSpans.TryRemove(messageContext.Id, out var span))
            {
                span.End();
                if (Elastic.Apm.Agent.Tracer.CurrentTransaction != null &&
                    Elastic.Apm.Agent.Tracer.CurrentTransaction.Name == "QueuePublish")
                {
                    Elastic.Apm.Agent.Tracer.CurrentTransaction.End();
                }
            }

            return Task.FromResult(new QueuePublishResult());
        }

        public Task<bool> OnBeforeReceiveAsync(object message, QueueMessageContext messageContext)
        {
            var transaction = Elastic.Apm.Agent.Tracer.CurrentTransaction ??
                              Elastic.Apm.Agent.Tracer.StartTransaction("QueueRequest", ApiConstants.TypeRequest);
            _transactions.TryAdd(messageContext.Id, transaction);
            return Task.FromResult(true);
        }

        public Task OnAfterReceiveAsync(object message, QueueMessageContext messageContext)
        {
            if (_transactions.TryRemove(messageContext.Id, out var transaction))
            {
                transaction.End();
            }
            return Task.FromResult(true);
        }
    }
}
