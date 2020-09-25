using System;
using System.Threading.Tasks;
using Grpc.Core;

namespace Sitko.Core.Grpc.Client.Discovery
{
    public interface IGrpcServiceAddressResolver<T> where T : ClientBase<T>
    {
        public Task InitAsync();
        public Uri? GetAddress();
    }
}
