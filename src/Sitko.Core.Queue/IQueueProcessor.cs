using System.Threading.Tasks;

namespace Sitko.Core.Queue
{
    public interface IQueueProcessor<in T> : IQueueProcessor where T : class, new()
    {
        Task<bool> ProcessAsync(T message, QueueMessageContext queueMessageContext);
    }

    public interface IQueueProcessor
    {
    }

    public interface IQueueProcessorHost
    {
        Task StartAsync();
        Task StopAsync();
    }
}
