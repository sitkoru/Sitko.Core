using System.Threading.Tasks;

namespace Sitko.Core.Grpc.Server
{
    public interface IGrpcServicesRegistrar
    {
        Task RegisterAsync<T>() where T : class;
        Task<bool> IsRegistered<T>() where T : class;
    }
}
