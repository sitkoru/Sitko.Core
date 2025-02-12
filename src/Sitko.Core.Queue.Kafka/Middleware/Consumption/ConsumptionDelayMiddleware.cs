using KafkaFlow;

namespace Sitko.Core.Queue.Kafka.Middleware.Consumption;

public class ConsumptionDelayMiddleware
    (TimeSpan delay)
    : IMessageMiddleware
{
    public async Task Invoke(IMessageContext context, MiddlewareDelegate next)
    {
        await Task.Delay(delay).ConfigureAwait(false);
        await next(context).ConfigureAwait(false);
    }
}
