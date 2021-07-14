using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Repository.EntityFrameworkCore
{
    public class EFRepositoriesModule<TAssembly> : RepositoriesModule<TAssembly, EFRepositoriesModuleOptions>
    {
        public override string OptionsKey => $"Repositories:EF:{typeof(TAssembly).Name}";

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            EFRepositoriesModuleOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddScoped(typeof(EFRepositoryContext<,,>));

            services.Scan(s =>
                s.FromAssemblyOf<TAssembly>().AddClasses(classes => classes.AssignableTo(typeof(EFRepository<,,>)))
                    .AsSelfWithInterfaces().WithScopedLifetime());
        }
    }

    public class EFRepositoriesModuleOptions : BaseModuleOptions
    {
    }
}
