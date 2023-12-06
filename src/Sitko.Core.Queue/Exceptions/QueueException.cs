namespace Sitko.Core.Queue.Exceptions;

public class QueueException : Exception;

public class QueueRequestTimeoutException : QueueException
{
    public QueueRequestTimeoutException(TimeSpan timeout) => Timeout = timeout;
    public TimeSpan Timeout { get; }
}

