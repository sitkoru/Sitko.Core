using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Repository
{
    public interface IRepositoriesModule
    {
    }

    public abstract class RepositoriesModule<TAssembly, TConfig> : BaseApplicationModule<TConfig>, IRepositoriesModule
        where TConfig : BaseModuleConfig, new()
    {
        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TConfig startupConfig)
        {
            base.ConfigureServices(context, services, startupConfig);
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
