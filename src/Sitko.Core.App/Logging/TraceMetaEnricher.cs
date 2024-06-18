using Serilog.Core;
using Serilog.Events;

namespace Sitko.Core.App.Logging;

// TODO: Убрать после обновления на Serilog.Extensions.Logging 8.0.1+
public class TraceMetaEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.TraceId != default)
        {
            logEvent.AddPropertyIfAbsent(new LogEventProperty("TraceId", new ScalarValue(logEvent.TraceId)));
        }

        if (logEvent.SpanId != default)
        {
            logEvent.AddPropertyIfAbsent(new LogEventProperty("SpanId", new ScalarValue(logEvent.SpanId)));
        }
    }
}
