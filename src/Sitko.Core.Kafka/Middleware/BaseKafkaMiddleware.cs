using KafkaFlow;

namespace Sitko.Core.Kafka.Middleware;

public abstract class BaseKafkaMiddleware : IMessageMiddleware
{
    public async Task Invoke(IMessageContext context, MiddlewareDelegate next)
    {
        try
        {
            var shouldStop = HandleBefore(context.Message.Value, context);
            if (!shouldStop)
            {
                await next(context).ConfigureAwait(false);
                HandleSuccess(context.Message.Value, context);
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, context.Message.Value, context);
            throw;
        }
        finally
        {
            HandleFinally(context.Message.Value, context);
        }
    }

    protected virtual void HandleFinally(object message, IMessageContext context)
    {
    }

    protected virtual void HandleError(Exception exception, object message, IMessageContext context)
    {
    }

    protected virtual void HandleSuccess(object message, IMessageContext context)
    {
    }

    protected virtual bool HandleBefore(object message, IMessageContext context) => true;
}
