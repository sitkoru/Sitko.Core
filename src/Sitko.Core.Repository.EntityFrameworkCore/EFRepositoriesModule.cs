using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Repository.EntityFrameworkCore
{
    public class EFRepositoriesModule<T> : RepositoriesModule<T, EFRepositoriesModuleConfig>
    {
        public override string GetConfigKey()
        {
            return $"Repositories:EF:{typeof(T).Name}";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            EFRepositoriesModuleConfig startupConfig)
        {
            base.ConfigureServices(context, services, startupConfig);
            services.AddScoped(typeof(EFRepositoryContext<,,>));
            services.AddScoped<EFRepositoryLock>();

            services.Scan(s =>
                s.FromAssemblyOf<T>().AddClasses(classes => classes.AssignableTo(typeof(EFRepository<,,>)))
                    .AsSelfWithInterfaces().WithScopedLifetime());
        }
    }

    public class EFRepositoriesModuleConfig : BaseModuleConfig
    {
        public bool EnableThreadSafeOperations { get; set; }
    }
}
