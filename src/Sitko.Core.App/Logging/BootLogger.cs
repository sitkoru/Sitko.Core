using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.App.Logging;

public class BootLogger<T> : IBootLogger<T>
{
    private readonly ILogger<T> logger;
    private readonly IApplicationContext applicationContext;
    private readonly ILogger bootLogger;

    // ReSharper disable once ContextualLoggerProblem
    public BootLogger(ILogger<T> logger, IApplicationContext applicationContext,
        [FromKeyedServices("BootLogger")] ILogger bootLogger)
    {
        this.logger = logger;
        this.applicationContext = applicationContext;
        this.bootLogger = bootLogger;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        logger.Log(logLevel, eventId, state, exception, formatter);
        if (!applicationContext.IsDevelopment())
        {
            bootLogger.Log(logLevel, eventId, state, exception, formatter);
        }
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => logger.BeginScope(state);
}
