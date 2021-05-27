using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Repository.EntityFrameworkCore
{
    public class EFRepositoriesModule<T> : RepositoriesModule<T, EfRepositoriesModuleOptions>
    {
        public override string GetOptionsKey()
        {
            return $"Repositories:EF:{typeof(T).Name}";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            EfRepositoriesModuleOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddScoped(typeof(EFRepositoryContext<,,>));

            services.Scan(s =>
                s.FromAssemblyOf<T>().AddClasses(classes => classes.AssignableTo(typeof(EFRepository<,,>)))
                    .AsSelfWithInterfaces().WithScopedLifetime());
        }
    }

    public class EfRepositoriesModuleOptions : BaseModuleOptions
    {
    }
}
