using System.Linq;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Queue
{
    public static class LoggingExtensions
    {
        /// <summary>
        /// Formats and writes an queue publish error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="queuePublishResult">The publish result to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogError(queuePublishResult, "Error while processing request from {Address}", address)</example>
        public static void LogError(this ILogger logger, QueuePublishResult queuePublishResult, string message,
            params object[] args)
        {
            var newArgs = args.Append(queuePublishResult.GetErrorText());
            if (queuePublishResult.Exception != null)
            {
                logger.LogError(queuePublishResult.Exception, "{Message}: {ErrorText}", message, newArgs);
            }
            else
            {
                logger.LogError("{Message}: {ErrorText}", message, newArgs);
            }
        }
    }
}
