using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.Grpc.Client.Discovery;

namespace Sitko.Core.Grpc.Client.External;

public class ExternalGrpcClientModule<TClient> : GrpcClientModule<TClient,
    ExternalGrpcServiceAddressResolver<TClient>,
    ExternalGrpcClientModuleOptions<TClient>>
    where TClient : ClientBase<TClient>
{
    public override string OptionsKey => $"Grpc:Client:External:{typeof(TClient).Name}";

    public override string[] OptionKeys => new[] { "Grpc:Client:External:Default", OptionsKey };

    protected override void RegisterResolver(IServiceCollection services,
        ExternalGrpcClientModuleOptions<TClient> config) =>
        services.AddSingleton<IGrpcServiceAddressResolver<TClient>>(
            new ExternalGrpcServiceAddressResolver<TClient>(config.Address));
}

public class ExternalGrpcClientModuleOptions<TClient> : GrpcClientModuleOptions<TClient>
    where TClient : ClientBase<TClient>
{
    public Uri Address { get; set; } = new("http://localhost");
}

