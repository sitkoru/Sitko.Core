using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Repository
{
    public class RepositoryContext<TEntity, TEntityPk, TDbContext> where TEntity : class, IEntity<TEntityPk>
        where TDbContext : DbContext
    {
        private readonly ILoggerFactory _loggerFactory;

        public RepositoryContext(TDbContext dbContext,
            ILoggerFactory loggerFactory,
            IEnumerable<IRepositoryFilter> filters = default,
            IEnumerable<IValidator<TEntity>> validators = default,
            IEnumerable<IAccessChecker<TEntity, TEntityPk>> accessCheckers = default)
        {
            _loggerFactory = loggerFactory;
            DbContext = dbContext;
            Filters = filters?.ToList();
            Validators = validators?.ToList();
            AccessCheckers = accessCheckers?.ToList();
        }

        internal TDbContext DbContext { get; }

        public ILogger<Repository<TEntity, TEntityPk, TDbContext>> Logger =>
            _loggerFactory.CreateLogger<Repository<TEntity, TEntityPk, TDbContext>>();

        public List<IRepositoryFilter> Filters { get; }
        public List<IValidator<TEntity>> Validators { get; }
        public List<IAccessChecker<TEntity, TEntityPk>> AccessCheckers { get; }
    }
}
