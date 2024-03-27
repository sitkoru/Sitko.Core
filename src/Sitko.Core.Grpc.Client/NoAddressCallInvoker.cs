using Grpc.Core;

namespace Sitko.Core.Grpc.Client;

internal class NoAddressCallInvoker(string serviceName) : CallInvoker
{
    public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host,
        CallOptions options, TRequest request) => throw new NoAddressCallException(serviceName);

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method,
        string? host, CallOptions options, TRequest request) => throw new NoAddressCallException(serviceName);

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method, string? host, CallOptions options,
        TRequest request) => throw new NoAddressCallException(serviceName);

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method, string? host, CallOptions options) =>
        throw new NoAddressCallException(serviceName);

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method, string? host, CallOptions options) =>
        throw new NoAddressCallException(serviceName);

    private class NoAddressCallException(string serviceName)
        : InvalidOperationException($"Can't resolve address for service {serviceName}");
}
