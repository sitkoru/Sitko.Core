using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Repository.Remote;

public class HttpRepositoryTransportModule : BaseApplicationModule<HttpRepositoryTransportOptions>
{
    public override string OptionsKey => "Repositories:Remote:Transports:Http";

    public override void ConfigureServices(IApplicationContext context, IServiceCollection services,
        HttpRepositoryTransportOptions startupOptions)
    {
        base.ConfigureServices(context, services, startupOptions);
        RegisterHttpClient(context, services, startupOptions);
        services.AddSingleton<IRemoteRepositoryTransport, HttpRepositoryTransport>();
    }

    protected virtual IHttpClientBuilder RegisterHttpClient(IApplicationContext context, IServiceCollection services,
        HttpRepositoryTransportOptions startupOptions) => services.AddHttpClient(nameof(HttpRepositoryTransport));
}
