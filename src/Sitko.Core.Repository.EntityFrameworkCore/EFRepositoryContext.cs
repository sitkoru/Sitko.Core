using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sitko.FluentValidation.Graph;

namespace Sitko.Core.Repository.EntityFrameworkCore;

public class EFRepositoryContext<TEntity, TEntityPk, TDbContext> : IRepositoryContext<TEntity, TEntityPk>
    where TEntity : class, IEntity<TEntityPk> where TDbContext : DbContext where TEntityPk : notnull
{
    private readonly ILoggerFactory loggerFactory;

    public EFRepositoryContext(EFRepositoryDbContextProvider<TDbContext> dbContextProvider,
        RepositoryFiltersManager filtersManager,
        ILoggerFactory loggerFactory,
        EFRepositoryLock repositoryLock,
        FluentGraphValidator fluentGraphValidator,
        IEnumerable<IAccessChecker<TEntity, TEntityPk>>? accessCheckers = null)
    {
        this.loggerFactory = loggerFactory;
        DbContext = dbContextProvider.DbContext;
        FiltersManager = filtersManager;
        RepositoryLock = repositoryLock;
        FluentGraphValidator = fluentGraphValidator;
        AccessCheckers = accessCheckers?.ToList();
    }

    internal TDbContext DbContext { get; }
    public EFRepositoryLock RepositoryLock { get; }
    public FluentGraphValidator FluentGraphValidator { get; }

    public ILogger<IRepository<TEntity, TEntityPk>> Logger =>
        loggerFactory.CreateLogger<EFRepository<TEntity, TEntityPk, TDbContext>>();

    public RepositoryFiltersManager FiltersManager { get; }
    public List<IAccessChecker<TEntity, TEntityPk>>? AccessCheckers { get; }
}

