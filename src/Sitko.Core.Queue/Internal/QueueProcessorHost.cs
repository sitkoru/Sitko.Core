using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Queue.Internal
{
    internal class QueueProcessorHost<T> : IQueueProcessorHost where T : class, new()
    {
        private readonly IQueue _queue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<QueueProcessorHost<T>> _logger;
        private QueueSubscribeResult _subscriptionResult;

        public QueueProcessorHost(IQueue queue, IServiceProvider serviceProvider, ILogger<QueueProcessorHost<T>> logger)
        {
            _queue = queue;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync()
        {
            _logger.LogInformation("Start processing messages of type {Type}", typeof(T));
            var result = await _queue.SubscribeAsync<T>(async (message, context) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var processors = scope.ServiceProvider.GetServices<IQueueProcessor<T>>();
                foreach (IQueueProcessor<T> processor in processors)
                {
                    try
                    {
                        await processor.ProcessAsync(message, context);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error while processing message {MessageType}: {ErrorText}", typeof(T),
                            e.ToString());
                    }
                }

                return true;
            });

            if (result.IsSuccess)
            {
                _subscriptionResult = result;
            }
            else
            {
                throw new Exception($"Can't subscribe to messages of type {typeof(T)}");
            }
        }

        public async Task StopAsync()
        {
            if (_subscriptionResult != null)
            {
                await _queue.UnsubscribeAsync<T>(_subscriptionResult.SubscriptionId);
            }
        }
    }
}
