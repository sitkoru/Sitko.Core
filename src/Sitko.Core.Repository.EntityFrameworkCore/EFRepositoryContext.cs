using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Repository.EntityFrameworkCore
{
    public class EFRepositoryContext<TEntity, TEntityPk, TDbContext> : IRepositoryContext<TEntity, TEntityPk>
        where TEntity : class, IEntity<TEntityPk> where TDbContext : DbContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoggerFactory _loggerFactory;

        public EFRepositoryContext(IServiceProvider serviceProvider, TDbContext dbContext, RepositoryFiltersManager filtersManager,
            ILoggerFactory loggerFactory, EFRepositoryLock? repositoryLock = null,
            IEnumerable<IValidator>? validators = null,
            IEnumerable<IAccessChecker<TEntity, TEntityPk>>? accessCheckers = null)
        {
            _serviceProvider = serviceProvider;
            _loggerFactory = loggerFactory;
            DbContext = dbContext;
            FiltersManager = filtersManager;
            RepositoryLock = repositoryLock;
            Validators = validators?.ToList();
            AccessCheckers = accessCheckers?.ToList();
        }

        internal TDbContext DbContext { get; }

        public ILogger<IRepository<TEntity, TEntityPk>> Logger =>
            _loggerFactory.CreateLogger<EFRepository<TEntity, TEntityPk, TDbContext>>();

        public RepositoryFiltersManager FiltersManager { get; }
        public EFRepositoryLock? RepositoryLock { get; }
        public List<IValidator>? Validators { get; }
        public List<IAccessChecker<TEntity, TEntityPk>>? AccessCheckers { get; }

        public IServiceScope CreateScope()
        {
            return _serviceProvider.CreateScope();
        }
    }
}
