using Grpc.Core;

namespace Sitko.Core.Grpc.Client.External;

public class ExternalGrpcClientModule<TClient> : GrpcClientModule<TClient,
    ExternalGrpcClientModuleOptions<TClient>>
    where TClient : ClientBase<TClient>
{
    public override string OptionsKey => $"Grpc:Client:External:{typeof(TClient).Name}";

    public override string[] OptionKeys => ["Grpc:Client:External:Default", OptionsKey];

    protected override Uri GenerateAddress(ExternalGrpcClientModuleOptions<TClient> options) => options.Address;
}

public class ExternalGrpcClientModuleOptions<TClient> : GrpcClientModuleOptions<TClient>
    where TClient : ClientBase<TClient>
{
    public Uri Address { get; set; } = new("http://localhost");
}
