using Grpc.Core;

namespace Sitko.Core.Grpc;

public static class GrpcServicesHelper
{
    public static string GetServiceName<T>()
    {
        var serviceName = typeof(T).BaseType?.DeclaringType?.Name;
        if (string.IsNullOrEmpty(serviceName))
        {
            throw new InvalidOperationException($"Can't find service name for {typeof(T)}");
        }

        return serviceName;
    }

    public static string GetServiceNameForClient<TClient>() where TClient : ClientBase<TClient> =>
        typeof(TClient).BaseType!.GenericTypeArguments.First().DeclaringType!.Name;
}
