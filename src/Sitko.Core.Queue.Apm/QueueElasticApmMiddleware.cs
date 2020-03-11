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
            var transaction = Elastic.Apm.Agent.Tracer.CurrentTransaction;
            if (transaction == null)
            {
                transaction = Elastic.Apm.Agent.Tracer.StartTransaction("QueuePublish", ApiConstants.TypeExternal);
                transaction.Labels.Add("MessageType", message.GetType().FullName);
                _transactions.TryAdd(messageContext.Id, transaction);
            }

            var span = transaction.StartSpan("PublishMessage", ApiConstants.TypeExternal, "Queue");
            span.Labels.Add("MessageType", message.GetType().FullName);
            _messageSpans.TryAdd(messageContext.Id, span);
            return Task.FromResult(new QueuePublishResult());
        }

        public Task OnAfterPublishAsync(object message, QueueMessageContext messageContext)
        {
            EndOperation(messageContext.Id);
            return Task.FromResult(new QueuePublishResult());
        }

        public Task<bool> OnBeforeReceiveAsync(object message, QueueMessageContext messageContext)
        {
            if (Elastic.Apm.Agent.Tracer.CurrentTransaction == null)
            {
                var transaction = Elastic.Apm.Agent.Tracer.StartTransaction("QueueRequest", ApiConstants.TypeRequest);
                transaction.Labels.Add("MessageType", message.GetType().FullName);
                _transactions.TryAdd(messageContext.Id, transaction);
            }
            else
            {
                var span = Elastic.Apm.Agent.Tracer.CurrentTransaction.StartSpan("ProcessMessage",
                    ApiConstants.TypeExternal, "Queue");
                span.Labels.Add("MessageType", message.GetType().FullName);
                _messageSpans.TryAdd(messageContext.Id, span);
            }


            return Task.FromResult(true);
        }

        private void EndOperation(Guid id)
        {
            if (_transactions.TryRemove(id, out var transaction))
            {
                transaction.End();
            }

            if (_messageSpans.TryRemove(id, out var span))
            {
                span.End();
            }
        }

        public Task OnAfterReceiveAsync(object message, QueueMessageContext messageContext)
        {
            EndOperation(messageContext.Id);
            return Task.FromResult(true);
        }
    }
}
