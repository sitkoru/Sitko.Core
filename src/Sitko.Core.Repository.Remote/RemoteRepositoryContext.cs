using Microsoft.Extensions.Logging;
using Sitko.FluentValidation.Graph;

namespace Sitko.Core.Repository.Remote;

public class RemoteRepositoryContext<TEntity, TEntityPk> : IRepositoryContext<TEntity, TEntityPk>
    where TEntity : class, IEntity<TEntityPk>
{
    private readonly ILoggerFactory loggerFactory;

    public RemoteRepositoryContext(
        RepositoryFiltersManager filtersManager,
        ILoggerFactory loggerFactory,
        FluentGraphValidator fluentGraphValidator,
        IRemoteRepositoryTransport? repositoryTransport = null,
        IEnumerable<IAccessChecker<TEntity, TEntityPk>>? accessCheckers = null)
    {
        this.loggerFactory = loggerFactory;
        FiltersManager = filtersManager;
        FluentGraphValidator = fluentGraphValidator;
        AccessCheckers = accessCheckers?.ToList();
        RepositoryTransport = repositoryTransport ?? throw new InvalidOperationException("You need to connect remote repository transport module");
    }

    public FluentGraphValidator FluentGraphValidator { get; }

    public  IRemoteRepositoryTransport RepositoryTransport { get; }

    public ILogger<IRepository<TEntity, TEntityPk>> Logger =>
        loggerFactory.CreateLogger<BaseRemoteRepository<TEntity, TEntityPk>>();

    public RepositoryFiltersManager FiltersManager { get; }
    public List<IAccessChecker<TEntity, TEntityPk>>? AccessCheckers { get; }
}
