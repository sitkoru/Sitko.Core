using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sitko.Core.App;

[assembly: InternalsVisibleTo("Sitko.Core.Repository.EntityFrameworkCore.Tests")]

namespace Sitko.Core.Repository.EntityFrameworkCore;

public class EFRepositoriesModule : RepositoriesModule<EFRepositoriesModuleOptions, IEFRepository>
{
    public override string OptionsKey => "Repositories:EF";

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        EFRepositoriesModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.TryAddScoped(typeof(EFRepositoryContext<,,>));
        services.TryAddScoped<EFRepositoryLock>();
    }
}

public class EFRepositoriesModuleOptions : RepositoriesModuleOptions<IEFRepository>;

