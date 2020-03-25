using System;
using System.Threading.Tasks;
using Grpc.Core;

namespace Sitko.Core.Grpc.Client
{
    public interface IGrpcServiceAddressResolver<T> where T : ClientBase<T>
    {
        public Task InitAsync();
        public Uri? GetAddress();
    }
}
