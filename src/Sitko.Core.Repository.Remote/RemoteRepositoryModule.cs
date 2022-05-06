using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sitko.Core.App;

[assembly: InternalsVisibleTo("Sitko.Core.Repository.Remote.Tests")]

namespace Sitko.Core.Repository.Remote;

public class RemoteRepositoryModule : RepositoriesModule<RemoteRepositoryOptions, IRemoteRepository>
{
    public override string OptionsKey => "Repositories:Remote";

    public override void ConfigureServices(IApplicationContext context, IServiceCollection services,
        RemoteRepositoryOptions startupOptions)
    {
        base.ConfigureServices(context, services, startupOptions);
        services.TryAddScoped(typeof(RemoteRepositoryContext<,>));
    }
}
