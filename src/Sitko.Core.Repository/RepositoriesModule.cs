using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Scrutor;
using Sitko.Core.App;

namespace Sitko.Core.Repository;

public interface IRepositoriesModule
{
}

public abstract class RepositoriesModule<TConfig, TRepositoryType> : BaseApplicationModule<TConfig>, IRepositoriesModule
    where TConfig : RepositoriesModuleOptions<TRepositoryType>, new() where TRepositoryType : IRepository
{
    public override bool AllowMultiple => true;

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TConfig startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.TryAddScoped<RepositoryFiltersManager>();

        var types = new List<Type>(startupOptions.Repositories);
        if (startupOptions.Assemblies.Count > 0)
        {
            foreach (var assembly in startupOptions.Assemblies)
            {
                types.AddRange(assembly.ExportedTypes.Where(type => typeof(TRepositoryType).IsAssignableFrom(type)));
            }
        }

        var assemblies = types.Select(type => type.Assembly).ToHashSet();
        foreach (var assembly in startupOptions.Assemblies)
        {
            assemblies.Add(assembly);
        }

        var entityTypes = types.Distinct().Select(type =>
                type
                    .GetInterfaces().First(implementedInterface =>
                        implementedInterface.Name == $"{nameof(IRepository)}`2" &&
                        implementedInterface.GenericTypeArguments.Length == 2))
            .Select(implementedInterface => (implementedInterface.GenericTypeArguments[0],
                implementedInterface.GenericTypeArguments[1])).ToList();
        services.Scan(s =>
            s.FromTypes(types.Distinct()).UsingRegistrationStrategy(RegistrationStrategy.Skip).AsSelfWithInterfaces()
                .WithScopedLifetime());
        foreach (var (entityType, entityPkType) in entityTypes)
        {
            var accessCheckerType = typeof(IAccessChecker<,>).MakeGenericType(entityType, entityPkType);
            var validatorType = typeof(IValidator<>).MakeGenericType(entityType);
            services.Scan(s =>
                s.FromAssemblies(assemblies)
                    .AddClasses(classes => classes.AssignableTo(accessCheckerType))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces().WithScopedLifetime());
            services.Scan(s =>
                s.FromAssemblies(assemblies)
                    .AddClasses(classes => classes.AssignableTo(validatorType))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces().WithScopedLifetime());
        }
    }
}

public class RepositoriesModuleOptions<TRepositoryType> : BaseModuleOptions where TRepositoryType : IRepository
{
    internal List<Assembly> Assemblies { get; } = new();
    internal List<Type> Repositories { get; } = new();

    public RepositoriesModuleOptions<TRepositoryType> AddRepositoriesFromAssemblyOf<TAssembly>()
    {
        Assemblies.Add(typeof(TAssembly).Assembly);
        return this;
    }

    public RepositoriesModuleOptions<TRepositoryType> AddRepository<TRepository>() where TRepository : TRepositoryType
    {
        Repositories.Add(typeof(TRepository));
        return this;
    }
}

