using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Queue.Internal
{
    public class QueueContext
    {
        public readonly List<IQueueMiddleware> Middlewares = new List<IQueueMiddleware>();
        public readonly List<IQueueMessageOptions> MessageOptions = new List<IQueueMessageOptions>();

        public QueueContext(IEnumerable<IQueueMiddleware> middlewares = default,
            IEnumerable<IQueueMessageOptions> messageOptions = default)
        {
            if (middlewares != null)
            {
                Middlewares.AddRange(middlewares);
            }

            if (messageOptions != null)
            {
                MessageOptions.AddRange(messageOptions);
            }
        }
    }

    internal class QueueService : IHostedService
    {
        private readonly ILogger<QueueService> _logger;
        private readonly List<IQueueProcessorHost> _processorHosts = new List<IQueueProcessorHost>();


        public QueueService(ILogger<QueueService> logger,
            IEnumerable<IQueueProcessorHost> processorHosts = null)
        {
            _logger = logger;
            if (processorHosts != null)
            {
                _processorHosts.AddRange(processorHosts);
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting message bus");
            foreach (IQueueProcessorHost processorHost in _processorHosts)
            {
                try
                {
                    await processorHost.StartAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Can't start processor host for type {Type}: {ErrorMessage}",
                        processorHost.GetType(), ex.ToString());
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping message bus");
            foreach (IQueueProcessorHost processorHost in _processorHosts)
            {
                await processorHost.StopAsync();
            }
        }
    }
}
