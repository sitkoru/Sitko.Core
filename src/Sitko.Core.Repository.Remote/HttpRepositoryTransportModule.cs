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
        services.AddHttpClient();
        services.AddSingleton<IRemoteRepositoryTransport, HttpRepositoryTransport>();
    }
}
