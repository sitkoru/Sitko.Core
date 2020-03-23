using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Repository
{
    public interface IRepositoriesModule
    {
    }

    public abstract class RepositoriesModule<T> : BaseApplicationModule, IRepositoriesModule
    {
        protected RepositoriesModule(BaseApplicationModuleConfig config, Application application) : base(config,
            application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddScoped<RepositoryFiltersManager>();
            services.Scan(s =>
                s.FromAssemblyOf<T>().AddClasses(classes => classes.AssignableTo(typeof(IValidator<>)))
                    .AsSelfWithInterfaces().WithScopedLifetime());
            services.Scan(s =>
                s.FromAssemblyOf<T>().AddClasses(classes => classes.AssignableTo(typeof(IAccessChecker<,>)))
                    .AsSelfWithInterfaces().WithScopedLifetime());
        }
    }
}
