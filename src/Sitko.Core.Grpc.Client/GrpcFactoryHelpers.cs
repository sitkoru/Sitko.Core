using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.ClientFactory;

namespace Sitko.Core.Grpc.Client;

internal static class GrpcFactoryHelpers
{
    public static CallInvoker BuildInterceptors(
        CallInvoker callInvoker,
        IServiceProvider serviceProvider,
        GrpcClientFactoryOptions clientFactoryOptions,
        InterceptorScope scope)
    {
        CallInvoker resolvedCallInvoker;
        if (clientFactoryOptions.InterceptorRegistrations.Count == 0)
        {
            resolvedCallInvoker = callInvoker;
        }
        else
        {
            List<Interceptor>? channelInterceptors = null;
            foreach (var registration in clientFactoryOptions.InterceptorRegistrations)
            {
                if (registration.Scope == scope)
                {
                    channelInterceptors ??= new List<Interceptor>();
                    channelInterceptors.Add(registration.Creator(serviceProvider));
                }
            }

            resolvedCallInvoker = channelInterceptors != null
                ? callInvoker.Intercept(channelInterceptors.ToArray())
                : callInvoker;
        }

        return resolvedCallInvoker;
    }
}
