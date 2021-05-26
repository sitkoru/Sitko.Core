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

    public abstract class RepositoriesModule<TAssembly, TConfig> : BaseApplicationModule<TConfig>, IRepositoriesModule
        where TConfig : BaseModuleConfig, new()
    {
        protected RepositoriesModule(Application application) : base(application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddScoped<RepositoryFiltersManager>();
            services.Scan(s =>
                s.FromAssemblyOf<TAssembly>().AddClasses(classes => classes.AssignableTo(typeof(IValidator<>)))
                    .AsSelfWithInterfaces().WithScopedLifetime());
            services.Scan(s =>
                s.FromAssemblyOf<TAssembly>()
                    .AddClasses(classes => classes.AssignableTo(typeof(IAccessChecker<,>)))
                    .AsSelfWithInterfaces().WithScopedLifetime());
        }
    }
}
