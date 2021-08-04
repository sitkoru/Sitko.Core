using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Repository.EntityFrameworkCore
{
    public class EFRepositoryContext<TEntity, TEntityPk, TDbContext> : IRepositoryContext<TEntity, TEntityPk>
        where TEntity : class, IEntity<TEntityPk> where TDbContext : DbContext
    {
        private readonly ILoggerFactory loggerFactory;

        public EFRepositoryContext(TDbContext dbContext,
            RepositoryFiltersManager filtersManager,
            ILoggerFactory loggerFactory,
            EFRepositoryLock repositoryLock,
            IEnumerable<IValidator>? validators = null,
            IEnumerable<IAccessChecker<TEntity, TEntityPk>>? accessCheckers = null)
        {
            this.loggerFactory = loggerFactory;
            DbContext = dbContext;
            FiltersManager = filtersManager;
            RepositoryLock = repositoryLock;
            Validators = validators?.ToList();
            AccessCheckers = accessCheckers?.ToList();
        }

        internal TDbContext DbContext { get; }
        public EFRepositoryLock RepositoryLock { get; }

        public ILogger<IRepository<TEntity, TEntityPk>> Logger =>
            loggerFactory.CreateLogger<EFRepository<TEntity, TEntityPk, TDbContext>>();

        public RepositoryFiltersManager FiltersManager { get; }
        public List<IValidator>? Validators { get; }
        public List<IAccessChecker<TEntity, TEntityPk>>? AccessCheckers { get; }
    }
}
