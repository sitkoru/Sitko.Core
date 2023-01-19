using Google.Protobuf;
using Grpc.Core;

namespace Sitko.Core.Grpc.Server;

// ReSharper disable once UnusedTypeParameter
public interface IGrpcCallProcessor<TService>
{
    Task<TResponse> ProcessCall<TResponse>(IMessage request,
        ServerCallContext context,
        Func<TResponse, GrpcCallResult> execute)
        where TResponse : class, IMessage, new();

    Task<TResponse> ProcessCallAsync<TResponse>(IMessage request,
        ServerCallContext context,
        Func<TResponse, Task<GrpcCallResult>> executeAsync)
        where TResponse : class, IMessage, new();

    Task<TResponse> ProcessCallAsync<TResponse>(IMessage request,
        ServerCallContext context,
        Func<TResponse, Task> executeAsync)
        where TResponse : class, IMessage, new();

    Task ProcessStreamAsync<TResponse>(IMessage request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        Func<Func<Action<TResponse>, Task>, Task> executeAsync)
        where TResponse : class, IMessage, new();

    Task<TResponse> ProcessStreamAsync<TResponse>(IAsyncStreamReader<IMessage> requestStream,
        ServerCallContext context,
        Func<TResponse, Task<GrpcCallResult>> executeAsync)
        where TResponse : class, IMessage, new();

    Task ProcessStreamAsync<TResponse>(IAsyncStreamReader<IMessage> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        Func<Func<Action<TResponse>, Task>, Task> executeAsync)
        where TResponse : class, IMessage, new();

    Task<bool> ValidateRequestAsync(IMessage request, IMessage response, ServerCallContext context);
}
