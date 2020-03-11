using System.Collections.Generic;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Repository.EntityFrameworkCore
{
    public class EFRepositoryContext<TEntity, TEntityPk, TDbContext> : IRepositoryContext<TEntity, TEntityPk>
        where TEntity : class, IEntity<TEntityPk> where TDbContext : DbContext
    {
        private readonly ILoggerFactory _loggerFactory;

        public EFRepositoryContext(TDbContext dbContext, RepositoryFiltersManager filtersManager,
            ILoggerFactory loggerFactory, List<IValidator<TEntity>>? validators = null,
            List<IAccessChecker<TEntity, TEntityPk>>? accessCheckers = null)
        {
            _loggerFactory = loggerFactory;
            DbContext = dbContext;
            FiltersManager = filtersManager;
            Validators = validators;
            AccessCheckers = accessCheckers;
        }

        internal TDbContext DbContext { get; }

        public ILogger<IRepository<TEntity, TEntityPk>> Logger =>
            _loggerFactory.CreateLogger<EFRepository<TEntity, TEntityPk, TDbContext>>();

        public RepositoryFiltersManager FiltersManager { get; }
        public List<IValidator<TEntity>>? Validators { get; }
        public List<IAccessChecker<TEntity, TEntityPk>>? AccessCheckers { get; }
    }
}
