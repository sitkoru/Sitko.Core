namespace Sitko.Core.Queue
{
    public interface IQueueMessageOptions
    {
        
    }

    // ReSharper disable once UnusedTypeParameter
    public interface IQueueMessageOptions<T> : IQueueMessageOptions where T : class, new()
    {
        
    }
}
