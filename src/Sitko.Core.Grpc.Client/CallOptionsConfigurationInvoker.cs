using Grpc.Core;

namespace Sitko.Core.Grpc.Client;

internal sealed class CallOptionsConfigurationInvoker : CallInvoker
{
    private readonly IList<Action<CallOptionsContext>> callOptionsActions;
    private readonly CallInvoker innerInvoker;
    private readonly IServiceProvider serviceProvider;

    public CallOptionsConfigurationInvoker(CallInvoker innerInvoker,
        IList<Action<CallOptionsContext>> callOptionsActions, IServiceProvider serviceProvider)
    {
        this.innerInvoker = innerInvoker;
        this.callOptionsActions = callOptionsActions;
        this.serviceProvider = serviceProvider;
    }

    private CallOptions ResolveCallOptions(CallOptions callOptions)
    {
        var context = new CallOptionsContext(callOptions, serviceProvider);

        foreach (var t in callOptionsActions)
        {
            t(context);
        }

        return context.CallOptions;
    }

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method, string? host, CallOptions options) =>
        innerInvoker.AsyncClientStreamingCall(method, host, ResolveCallOptions(options));

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method, string? host, CallOptions options) =>
        innerInvoker.AsyncDuplexStreamingCall(method, host, ResolveCallOptions(options));

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) =>
        innerInvoker.AsyncServerStreamingCall(method, host, ResolveCallOptions(options), request);

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method,
        string? host, CallOptions options, TRequest request) =>
        innerInvoker.AsyncUnaryCall(method, host, ResolveCallOptions(options), request);

    public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host,
        CallOptions options, TRequest request) =>
        innerInvoker.BlockingUnaryCall(method, host, ResolveCallOptions(options), request);
}

