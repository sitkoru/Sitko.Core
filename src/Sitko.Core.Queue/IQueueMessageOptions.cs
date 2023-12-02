namespace Sitko.Core.Queue;

public interface IQueueMessageOptions;

// Generic interface is required for dependency injection
// ReSharper disable once UnusedTypeParameter
public interface IQueueMessageOptions<T> : IQueueMessageOptions where T : class;

