using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Repository.EntityFrameworkCore
{
    public class EFRepositoriesModule<T> : RepositoriesModule<T, EFRepositoriesModuleConfig>
    {
        public EFRepositoriesModule(EFRepositoriesModuleConfig config, Application application) : base(config,
            application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddScoped(typeof(EFRepositoryContext<,,>));

            services.Scan(s =>
                s.FromAssemblyOf<T>().AddClasses(classes => classes.AssignableTo(typeof(EFRepository<,,>)))
                    .AsSelfWithInterfaces().WithScopedLifetime());
        }
    }

    public class EFRepositoriesModuleConfig
    {
    }
}
