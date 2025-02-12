using System.Text;
using System.Text.Json;
using KafkaFlow;
using Sitko.Core.Queue.Kafka.Events;

namespace Sitko.Core.Queue.Kafka.Middleware;

public abstract class MessageMiddlewareBase : IMessageMiddleware
{
    protected JsonSerializerOptions JsonSerializerOptions { get; } = new();

    public async Task Invoke(IMessageContext context, MiddlewareDelegate next)
    {
        if (context.Message.Value is BaseEvent message)
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

    protected virtual void HandleFinally(BaseEvent message, IMessageContext context)
    {
    }

    protected virtual void HandleFail(Exception exception, BaseEvent message, IMessageContext context)
    {
    }

    protected virtual void HandleSuccess(BaseEvent message, IMessageContext context)
    {
    }

    protected virtual bool HandleBefore(BaseEvent message, IMessageContext context) => true;


    protected static string GetMessageKey(IMessageContext context)
    {
        var key = "";
        if (context.Message.Key is byte[] bytes)
        {
            key = Encoding.UTF8.GetString(bytes);
        }

        return key;
    }
}
