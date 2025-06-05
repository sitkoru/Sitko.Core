using Grpc.Core;
using Grpc.Net.Client.Balancer;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Client.External;

public class ExternalGrpcClientModule<TClient> : GrpcClientModule<TClient,
    ExternalGrpcClientModuleOptions<TClient>>
    where TClient : ClientBase<TClient>
{
    public override string OptionsKey => $"Grpc:Client:External:{typeof(TClient).Name}";

    public override string[] OptionKeys => ["Grpc:Client:External:Default", OptionsKey];

    protected override string ResolverFactoryScheme => "static";

    protected override ResolverFactory CreateResolverFactory(IServiceProvider sp)
    {
        var applicationContext = sp.GetRequiredService<IApplicationContext>();
        var resolver = ExternalGrpcClientModuleResolverFactory.GetOrCreate(applicationContext.Id);
        return resolver.Factory;
    }

    protected override void RegisterClient<TClientBase>(IApplicationContext applicationContext,
        ExternalGrpcClientModuleOptions<TClient> options)
    {
        var resolver = ExternalGrpcClientModuleResolverFactory.GetOrCreate(applicationContext.Id);
        resolver.Register<TClientBase>(options.Address);
    }
}

public class ExternalGrpcClientModuleOptions<TClient> : GrpcClientModuleOptions<TClient>
    where TClient : ClientBase<TClient>
{
    public Uri Address { get; set; } = new("http://localhost");
}
