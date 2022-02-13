using Microsoft.Extensions.Logging;
using Sitko.FluentValidation.Graph;

namespace Sitko.Core.Repository.Remote;

public class RemoteRepositoryContext<TEntity, TEntityPk> : IRepositoryContext<TEntity, TEntityPk>
    where TEntity : class, IEntity<TEntityPk>
{
    private readonly IRemoteRepositoryTransport repositoryTransport;
    private readonly ILoggerFactory loggerFactory;

    public RemoteRepositoryContext(
        IRemoteRepositoryTransport repositoryTransport,
        RepositoryFiltersManager filtersManager,
        ILoggerFactory loggerFactory,
        FluentGraphValidator fluentGraphValidator,
        IEnumerable<IAccessChecker<TEntity, TEntityPk>>? accessCheckers = null)
    {
        this.repositoryTransport = repositoryTransport;
        this.loggerFactory = loggerFactory;
        FiltersManager = filtersManager;
        FluentGraphValidator = fluentGraphValidator;
        AccessCheckers = accessCheckers?.ToList();
    }

    public FluentGraphValidator FluentGraphValidator { get; }

    public ILogger<IRepository<TEntity, TEntityPk>> Logger =>
        loggerFactory.CreateLogger<BaseRemoteRepository<TEntity, TEntityPk>>();

    public RepositoryFiltersManager FiltersManager { get; }
    public List<IAccessChecker<TEntity, TEntityPk>>? AccessCheckers { get; }
}
