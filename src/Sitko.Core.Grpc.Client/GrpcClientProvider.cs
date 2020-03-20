using System;
using System.Threading.Tasks;
using Grpc.Core;

namespace Sitko.Core.Grpc.Client
{
    public class GrpcClient<T> where T : ClientBase<T>
    {
        private T? _currentInstance;
        public bool IsReady { get; private set; }

        public void SetReady(bool ready)
        {
            IsReady = ready;
        }

        public void SetInstance(T instance)
        {
            _currentInstance = instance;
        }

        public T Instance
        {
            get
            {
                if (IsReady && _currentInstance != null)
                {
                    return _currentInstance;
                }

                throw new Exception($"Client if {typeof(T)} isn't ready");
            }
        }
    }

    public interface IGrpcClientProvider<T> where T : ClientBase<T>
    {
        Task<GrpcClient<T>> GetClientAsync();
    }
}
