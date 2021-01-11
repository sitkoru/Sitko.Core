namespace Sitko.Core.Queue
{
    public interface IQueueMessageOptions
    {
        
    }

    public interface IQueueMessageOptions<T> : IQueueMessageOptions where T : class
    {
        
    }
}
