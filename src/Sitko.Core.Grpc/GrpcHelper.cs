using System.Reflection;
using Google.Protobuf.Reflection;
using Grpc.Core;

namespace Sitko.Core.Grpc
{
    public static class GrpcServicesHelper
    {
        public static string GetServiceName<T>() => GetDescriptor(typeof(T).BaseType?.DeclaringType).FullName;

        public static string GetServiceNameForClient<TClient>() where TClient : ClientBase<TClient> =>
            GetDescriptor(typeof(TClient).DeclaringType).FullName;

        private static ServiceDescriptor GetDescriptor(Type? serviceType) =>
            serviceType?.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
                as ServiceDescriptor ?? throw new InvalidOperationException($"Cannot find protobuf service descriptor for {serviceType}");
    }
}
