using Grpc.Core;
using JetBrains.Annotations;
using Sitko.Core.ServiceDiscovery;

namespace Sitko.Core.Grpc.Client.Discovery;

[UsedImplicitly]
public class ServiceDiscoveryAddressResolver<TClient>(IServiceDiscoveryResolver resolver)
    : IGrpcServiceAddressResolver<TClient>
    where TClient : ClientBase<TClient>
{
    private Uri? currentAddress;

    public async Task InitAsync()
    {
        await resolver.LoadAsync();
        resolver.Subscribe(GrpcModuleConstants.GrpcServiceDiscoveryType,
            GrpcServicesHelper.GetServiceNameForClient<TClient>(), services =>
            {
                var service = services.OrderBy(_ => Guid.NewGuid()).First(); // Round Robin
                var url = new Uri($"{service.Scheme}://{service.Host}:{service.Port}");
                if (url != currentAddress)
                {
                    currentAddress = url;
                    OnChange?.Invoke(this, EventArgs.Empty);
                }
            });
    }

    public Uri? GetAddress() => currentAddress;

    public event EventHandler? OnChange;
}
