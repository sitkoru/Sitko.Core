using System.Text.Json;
using KafkaFlow;
using Sitko.Core.Queue.Kafka.Events;

namespace Sitko.Core.Queue.Kafka.Middleware;

public abstract class MessageMiddlewareBase : IMessageMiddleware
{
    protected JsonSerializerOptions JsonSerializerOptions { get; } = new();

    public async Task Invoke(IMessageContext context, MiddlewareDelegate next)
    {
        if (context.Message.Value is IBaseEvent message)
        {
            try
            {
                var needToContinue = HandleBefore(message, context);
                if (needToContinue)
                {
                    await next(context).ConfigureAwait(false);
                    HandleSuccess(message, context);
                }
            }
            catch (Exception ex)
            {
                HandleFail(ex, message, context);
                throw;
            }
            finally
            {
                HandleFinally(message, context);
            }
        }
    }

    protected virtual void HandleFinally(IBaseEvent message, IMessageContext context)
    {
    }

    protected virtual void HandleFail(Exception exception, IBaseEvent message, IMessageContext context)
    {
    }

    protected virtual void HandleSuccess(IBaseEvent message, IMessageContext context)
    {
    }

    protected virtual bool HandleBefore(IBaseEvent message, IMessageContext context) => true;
}
