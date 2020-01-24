using System;

namespace Sitko.Core.Queue.Exceptions
{
    public class QueueException : Exception
    {
    }

    public class QueueRequestTimeoutException : QueueException
    {
        public TimeSpan Timeout { get; }

        public QueueRequestTimeoutException(TimeSpan timeout)
        {
            Timeout = timeout;
        }
    }
}
